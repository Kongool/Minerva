using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Minerva;

namespace Minerva.GameSync;

/// <summary>
/// Feeds a <see cref="WorldState"/> from the live game each frame. Two data paths:
/// <list type="bullet">
/// <item><b>Polling</b> — persistent state (position, HP, targetable, cast progress, statuses,
/// shield, cast location) is diffed from Dalamud's managed object table + a few CS reads.</item>
/// <item><b>Packet hooks</b> — transient events that polling can't catch (overhead icons, VFX,
/// tethers, map/environment effects, director updates, RSV strings) are captured in detours and
/// queued, then drained on the next frame so all state mutation stays on the main thread.</item>
/// </list>
/// Not yet covered: the caster-side cast-resolved-with-full-target-list, which needs the
/// randomized-opcode packet-decoder subsystem (Phase 4 / replay). Cast start &amp; finish are
/// polled, so mechanics still draw; only "who exactly got hit" analysis is deferred.
/// </summary>
public sealed unsafe class WorldStateGameSync : IDisposable
{
    private readonly WorldState ws;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly DateTime startWall = DateTime.UtcNow;
    private uint frameIndex;

    private readonly HashSet<ulong> seenThisFrame = [];
    private readonly List<ulong> toRemove = [];
    private readonly HashSet<ulong> warnedActors = []; // warn-once per actor so a bad object doesn't spam the log

    // events captured in detours (main thread) and drained next Update
    private readonly List<WorldState.Operation> globalOps = [];
    private readonly Dictionary<ulong, List<WorldState.Operation>> actorOps = [];

    // --- hooks (signatures reused from BossmodReborn — the hard-won constants) ---
    private delegate void ActorControlDelegate(uint actorID, uint category, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetID, byte replaying);
    private readonly Hook<ActorControlDelegate>? actorControlHook;

    private delegate void MapEffectDelegate(nint self, uint index, ushort s1, ushort s2);
    private readonly Hook<MapEffectDelegate>? mapEffectHook;

    private delegate void RSVDataDelegate(byte* packet);
    private readonly Hook<RSVDataDelegate>? rsvHook;

    private delegate void ActorCastDelegate(uint casterID, ActorCastPacket* packet);
    private readonly Hook<ActorCastDelegate>? actorCastHook;

    /// <summary>
    /// Where each in-flight cast is aimed, straight off the wire. The game only stores a cast's target
    /// location in memory for *area*-targeted actions, so a boss self-casting — a line-of-sight mechanic, a
    /// cone, a raidwide — reads back (0,0,0). The packet carries it either way. Keyed by caster, cleared when
    /// the cast ends, so it never outgrows the actors on screen.
    /// </summary>
    private readonly Dictionary<ulong, Vector3> castPositions = [];

    /// <summary>
    /// The server's ActorCast packet, laid out as the game sends it. Only the trailing position matters here,
    /// but every preceding field has to be declared for the offsets to land.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ActorCastPacket
    {
        public ushort SpellID;
        public byte ActionType;
        public byte BaseCastTime100ms;
        public uint ActionID;
        public float CastTime;
        public uint TargetID;
        public ushort Rotation;
        public byte Interruptible;
        public byte U1;
        public uint BallistaEntityID;
        public ushort PosX;
        public ushort PosY;
        public ushort PosZ;
        public ushort U3;
    }

    // Resolved actions (ActionEffect). This is what turns a cast into a *cast event* — without it
    // OnEventCast never fires, NumCasts never increments, and every component that resolves on a landed
    // action stays stuck. Hooked by address from FFXIVClientStructs rather than a signature, matching BMR.
    private readonly Hook<ActionEffectHandler.Delegates.Receive>? actionEffectHook;

    public WorldStateGameSync(WorldState ws)
    {
        this.ws = ws;

        this.actorControlHook = this.TryHook<ActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", this.ActorControlDetour, "ActorControl");
        this.mapEffectHook = this.TryHook<MapEffectDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B FA 41 0F B7 E8", this.MapEffectDetour, "MapEffect");
        this.rsvHook = this.TryHook<RSVDataDelegate>("44 8B 09 4C 8D 41 34", this.RSVDataDetour, "RSVData");
        this.actorCastHook = this.TryHook<ActorCastDelegate>("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B D1", this.ActorCastDetour, "ActorCast");
        this.actionEffectHook = this.TryHookAddress<ActionEffectHandler.Delegates.Receive>(
            ActionEffectHandler.Addresses.Receive.Value, this.ActionEffectDetour, "ActionEffect");
    }

    /// <summary>Install a hook at a known address (FFXIVClientStructs), guarded like <see cref="TryHook"/>.</summary>
    private Hook<T>? TryHookAddress<T>(nint address, T detour, string name) where T : Delegate
    {
        try
        {
            var hook = Service.GameInterop.HookFromAddress(address, detour);
            hook.Enable();
            return hook;
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, $"Minerva: failed to install {name} hook. Continuing without it.");
            return null;
        }
    }

    private Hook<T>? TryHook<T>(string signature, T detour, string name) where T : Delegate
    {
        try
        {
            var hook = Service.GameInterop.HookFromSignature(signature, detour);
            hook.Enable();
            return hook;
        }
        catch (Exception ex)
        {
            // a stale signature must not take the whole plugin down — polling still works
            Service.Log.Warning(ex, $"Minerva: failed to install {name} hook (signature may be outdated). Continuing without it.");
            return null;
        }
    }

    public void Dispose()
    {
        this.actorControlHook?.Dispose();
        this.mapEffectHook?.Dispose();
        this.rsvHook?.Dispose();
    }

    /// <summary>Run one sync tick. Call from <c>IFramework.Update</c>.</summary>
    public void Update(TimeSpan frameDelta)
    {
        this.EmitFrameStart(frameDelta);
        this.EmitZoneChange();
        this.DrainGlobalOps();
        this.UpdateActors();
        this.SyncParty();
        this.SyncDutyActions();
        this.UpdateActiveFate();
        this.UpdateWaymarks();
    }

    /// <summary>
    /// Mirror the party's field markers into the world state.
    /// <para>Markers are how a party writes its plan onto the floor, and several ported modules resolve
    /// mechanics against them — "the tower on A", "stack on 1". Polled rather than hooked because there is
    /// no event for a marker moving, and the whole table is eight vectors.</para>
    /// <para>Only actual changes are emitted, so a recording carries a marker placement once rather than
    /// once per frame.</para>
    /// </summary>
    private void UpdateWaymarks()
    {
        var mc = FFXIVClientStructs.FFXIV.Client.Game.UI.MarkingController.Instance();
        if (mc == null)
            return;

        for (var i = 0; i < (int)Waymark.Count; ++i)
        {
            ref var mark = ref mc->FieldMarkers[i];
            Vector3? now = mark.Active ? new Vector3(mark.X / 1000f, mark.Y / 1000f, mark.Z / 1000f) : null;
            var before = this.ws.Waymarks[(Waymark)i];
            if (before is { } b && now is { } n)
            {
                if (b == n)
                    continue;
            }
            else if (before == null && now == null)
            {
                continue;
            }

            this.ws.Execute(new WaymarkState.OpWaymarkChange((Waymark)i, now));
        }
    }

    /// <summary>
    /// Publish the FATE the player is standing in, with the radius the game declares for it.
    /// <para>The point is the boundary. An open-world FATE is not sealed, so nothing stops the auto-dodge
    /// walking the character out of it — and out of it means out of the encounter. BossmodReborn does not
    /// bound its open-world modules and that is exactly what happens. A stated radius is the difference
    /// between a bound that keeps you in the fight and one that was invented.</para>
    /// </summary>
    private void UpdateActiveFate()
    {
        var current = this.ws.ActiveFate;
        var found = default(FateState);
        if (Service.ObjectTable[0] is { } me)
        {
            var p = new WPos(me.Position.X, me.Position.Z);
            foreach (var f in Service.FateTable)
            {
                if (f.Radius <= 0f)
                    continue;

                // the one being stood in, not merely the nearest: foray zones run several at once
                var c = new WPos(f.Position.X, f.Position.Z);
                if ((p - c).LengthSq() <= f.Radius * f.Radius)
                {
                    found = new FateState(f.FateId, c, f.Radius);
                    break;
                }
            }
        }

        if (found.ID != current.ID || MathF.Abs(found.Radius - current.Radius) > 0.5f)
            this.ws.Execute(new WorldState.OpActiveFate(found));
    }

    private void EmitFrameStart(TimeSpan frameDelta)
    {
        var qpc = (ulong)this.clock.ElapsedTicks;
        var timestamp = this.startWall.AddSeconds(this.clock.Elapsed.TotalSeconds);
        var dt = (float)frameDelta.TotalSeconds;
        var frame = new FrameState(timestamp, qpc, this.frameIndex++, dt, dt, 1f);
        this.ws.Execute(new WorldState.OpFrameStart(frame, frameDelta));
    }

    private void EmitZoneChange()
    {
        var zone = (ushort)Service.ClientState.TerritoryType;
        var cfc = GameData.CurrentContentFinderConditionId();
        if (this.ws.CurrentZone != zone || this.ws.CurrentCFCID != cfc)
            this.ws.Execute(new WorldState.OpZoneChange(zone, cfc));
    }

    private void DrainGlobalOps()
    {
        for (var i = 0; i < this.globalOps.Count; ++i)
            this.ws.Execute(this.globalOps[i]);
        this.globalOps.Clear();
    }

    private void UpdateActors()
    {
        this.seenThisFrame.Clear();

        var table = Service.ObjectTable;
        // object-table slot 0 is the POV; expose it so components' Raid.Player() resolves in-game
        this.ws.Party.PlayerInstanceID = table[0]?.GameObjectId ?? 0;
        var len = table.Length;
        for (var i = 0; i < len; ++i)
        {
            var obj = table[i];
            if (obj == null)
                continue;
            var id = obj.GameObjectId;
            if (id is 0 or 0xE0000000)
                continue;

            this.seenThisFrame.Add(id);
            // isolate per-actor: a single malformed game object must not abort the whole sync tick
            try
            {
                this.UpdateActor(obj, i);
                this.DispatchActorOps(id); // apply this actor's queued transient events now that it exists
            }
            catch (Exception ex)
            {
                if (this.warnedActors.Add(id))
                    Service.Log.Warning(ex, $"Minerva: skipped actor {id:X} this frame (bad game-object state).");
            }
        }

        // despawns: tracked but no longer in the table
        this.toRemove.Clear();
        foreach (var id in this.ws.Actors.Actors.Keys)
            if (!this.seenThisFrame.Contains(id))
                this.toRemove.Add(id);
        foreach (var id in this.toRemove)
        {
            this.DispatchActorOps(id);
            this.ws.Execute(new ActorState.OpDestroy(id));
        }

        // any events for actors that never showed up in the table this frame
        if (this.actorOps.Count > 0)
        {
            foreach (var (id, ops) in this.actorOps)
                Service.Log.Verbose($"Minerva: {ops.Count} queued events for unknown actor {id:X}");
            this.actorOps.Clear();
        }
    }

    private void DispatchActorOps(ulong id)
    {
        if (!this.actorOps.TryGetValue(id, out var ops))
            return;
        for (var i = 0; i < ops.Count; ++i)
            this.ws.Execute(ops[i]);
        this.actorOps.Remove(id);
    }

    private void UpdateActor(IGameObject obj, int index)
    {
        var id = obj.GameObjectId;
        var chr = obj as IBattleChara;
        var addr = obj.Address;

        var name = obj.Name.TextValue;
        var nameID = chr?.NameId ?? 0u;
        var posRot = new Vector4(obj.Position, obj.Rotation);
        var radius = obj.HitboxRadius;
        var targetable = obj.IsTargetable;

        var type = (ActorType)(((int)obj.ObjectKind << 8) + obj.SubKind);
        var hpmp = default(ActorHPMP);
        var inCombat = false;
        var isDead = false;
        var friendly = true;
        var target = 0ul;
        if (chr != null)
        {
            var shield = (uint)(GameData.ShieldPercent(addr) * 0.01f * chr.MaxHp);
            hpmp = new ActorHPMP(chr.CurrentHp, chr.MaxHp, shield, chr.CurrentMp, chr.MaxMp);
            inCombat = chr.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat);
            isDead = chr.IsDead;
            // Ally/enemy from the game's own classifier (matches BMR), not the flaky Hostile flag. Helpers are
            // the boss's invisible casters — never allies — and the game won't classify an untargetable helper
            // as an enemy, so exclude them explicitly.
            friendly = type != ActorType.Helper && !GameData.IsClassifiedEnemy(addr);
            target = SanitizeId(chr.TargetObjectId);
        }

        var mountId = GameData.TryMountId(addr, out var mid) ? mid : 0u;

        var existing = this.ws.Actors.Find(id);
        if (existing == null)
        {
            this.ws.Execute(new ActorState.OpCreate(id, obj.BaseId, index, name, nameID, type, posRot, radius, hpmp, targetable, friendly, SanitizeId(obj.OwnerId)));
            existing = this.ws.Actors.Find(id)!;
        }
        else
        {
            if (existing.Name != name || existing.NameID != nameID)
                this.ws.Execute(new ActorState.OpRename(id, name, nameID));
            if (existing.PosRot != posRot)
                this.ws.Execute(new ActorState.OpMove(id, posRot));
            if (existing.HitboxRadius != radius)
                this.ws.Execute(new ActorState.OpSizeChange(id, radius));
            if (existing.HPMP != hpmp)
                this.ws.Execute(new ActorState.OpHPMP(id, hpmp));
            if (existing.IsTargetable != targetable)
                this.ws.Execute(new ActorState.OpTargetable(id, targetable));
        }

        // set straight on the actor rather than through an op: nothing subscribes to a mount change, and
        // an op per frame for a value that only matters while it is read would be noise in every recording
        existing.MountId = mountId;

        if (existing.IsDead != isDead)
            this.ws.Execute(new ActorState.OpDead(id, isDead));
        if (existing.InCombat != inCombat)
            this.ws.Execute(new ActorState.OpCombat(id, inCombat));
        if (existing.TargetID != target)
            this.ws.Execute(new ActorState.OpTarget(id, target));

        var eventState = GameData.EventState(addr);
        if (existing.EventState != eventState)
            this.ws.Execute(new ActorState.OpEventState(id, eventState));
        var renderflags = GameData.RenderFlags(addr);
        if (existing.Renderflags != renderflags)
            this.ws.Execute(new ActorState.OpRenderflags(id, renderflags));

        if (chr != null)
        {
            var cls = (Class)(byte)chr.ClassJob.RowId;
            if (existing.Class != cls)
                this.ws.Execute(new ActorState.OpClassChange(id, cls));

            // Occult Crescent knowledge level: Forbidden Folios resolves against it, so a module reading a
            // stale zero would hand every player the same answer regardless of the mechanic.
            if (GameData.TryForayInfo(addr, out var forayLevel, out var forayElement))
            {
                var foray = new ActorForayInfo(forayLevel, forayElement);
                if (existing.ForayInfo != foray)
                    this.ws.Execute(new ActorState.OpForayInfo(id, foray));
            }

            this.UpdateCast(existing, chr, addr);
            this.UpdateStatuses(existing, chr, addr);
            this.UpdateIncomingEffects(existing, addr);
        }
    }

    /// <summary>
    /// Mirror the duty's granted actions.
    ///
    /// <para>These appear when a fight changes what you are — Wuk Lamat's kit in a Dawntrail solo duty, a
    /// Bozja lost action, the one button left while transformed. A module names one because in that fight
    /// it is the answer, sometimes the only usable thing; nothing about your job says so.</para>
    ///
    /// <para>Only the first two slots carry charges — the game added slots 3-5 without extending the
    /// charge arrays, so reading charges for those would walk off the end. They report 0/0, which reads as
    /// "no charge information" rather than "no charges left".</para>
    /// </summary>
    private unsafe void SyncDutyActions()
    {
        var dst = this.ws.Client.DutyActions;
        var dm = FFXIVClientStructs.FFXIV.Client.Game.DutyActionManager.GetInstanceIfReady();
        if (dm == null || !dm->ActionActive[0])
        {
            Array.Clear(dst);
            return;
        }

        for (var i = 0; i < ClientState.NumDutyActions; ++i)
        {
            if (i >= dm->NumValidSlots)
            {
                dst[i] = default;
                continue;
            }
            byte cur = 0, max = 0;
            if (i < 2)
            {
                cur = dm->CurCharges[i];
                max = dm->MaxCharges[i];
            }
            dst[i] = new ClientState.DutyAction(new ActionID(ActionType.Spell, dm->ActionId[i]), cur, max);
        }
    }

    // Mirror the game's party list into WorldState so components' Raid.WithSlot()/Player() resolve.
    // Solo: the local player occupies slot 0 (matching BMR's single-player handling).
    private void SyncParty()
    {
        var pl = Service.PartyList;
        var count = pl.Length;
        if (count == 0)
        {
            this.SetPartySlot(0, 0, Service.ObjectTable[0]?.GameObjectId ?? 0);
            for (var i = 1; i < PartyState.MaxSlots; ++i)
                this.SetPartySlot(i, 0, 0);
            return;
        }
        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            if (i < count && pl[i] is { } m)
                this.SetPartySlot(i, (ulong)m.ContentId, m.EntityId);
            else
                this.SetPartySlot(i, 0, 0);
        }
    }

    private void SetPartySlot(int slot, ulong contentId, ulong instanceId)
    {
        var cur = this.ws.Party.Slots[slot];
        if (cur.ContentID != contentId || cur.InstanceID != instanceId)
            this.ws.Execute(new PartyState.OpModify(slot, new PartyState.Member(contentId, instanceId)));
    }

    private void UpdateCast(Actor act, IBattleChara chr, nint addr)
    {
        ActorCastInfo? cur = null;
        // gate on the CS null-check first: Dalamud's IsCasting/CastActionId/… deref the cast-info block
        // without guarding it, and it's null whenever the actor isn't casting -> NRE inside the getter
        if (GameData.HasCastInfo(addr) && chr.IsCasting && chr.CastActionId != 0)
        {
            // prefer the packet's aim point; the in-memory field is only filled for area-targeted casts
            var location = this.castPositions.TryGetValue(act.InstanceID, out var packetLoc) ? packetLoc
                : GameData.TryCastLocation(addr, out var loc) ? loc
                : default;
            cur = new ActorCastInfo
            {
                Action = new ActionID((ActionType)chr.CastActionType, chr.CastActionId),
                TargetID = SanitizeId(chr.CastTargetObjectId),
                // The cast's own aim, not the caster's live facing -- see GameData.TryCastRotation.
                Rotation = (GameData.TryCastRotation(addr, out var castRot) ? castRot : chr.Rotation).Radians(),
                Location = location,
                ElapsedTime = chr.CurrentCastTime,
                TotalTime = chr.TotalCastTime,
            };
        }

        var prev = act.CastInfo;
        if (cur == null && prev == null)
            return;

        if (cur != null && prev != null && cur.Action == prev.Action && cur.TargetID == prev.TargetID
            && cur.TotalTime == prev.TotalTime && MathF.Abs(cur.ElapsedTime - prev.ElapsedTime) < 0.2f)
        {
            prev.ElapsedTime = cur.ElapsedTime;
            return;
        }

        if (cur == null)
            this.castPositions.Remove(act.InstanceID);

        this.ws.Execute(new ActorState.OpCastInfo(act.InstanceID, cur));
    }

    /// <summary>
    /// Mirror the game's incoming-effect ring for an actor.
    ///
    /// <para>Needed for fights that deliver a MARKER as an action rather than a status — nothing changes on
    /// the actor, no HP moves, an action id simply arrives against them, and this ring is the only place it
    /// is visible. Distinct from the pending-effect lists, which reshape the same feed into HP prediction.</para>
    ///
    /// <para>An op is emitted only when a slot's (sequence, target index) actually changes, so the volume
    /// tracks real events rather than frames — the ring is read every frame but is almost always unchanged.
    /// The slots are copied raw and undecoded, which is what makes an old recording replayable against code
    /// that later understands more of them than the code that captured it did.</para>
    /// </summary>
    private unsafe void UpdateIncomingEffects(Actor act, nint addr)
    {
        var chr = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)addr;
        if (chr == null)
            return;

        var aeh = chr->GetActionEffectHandler();
        if (aeh == null)
            return;

        var count = Math.Min(aeh->IncomingEffects.Length, Actor.NumIncomingEffects);
        for (var i = 0; i < count; ++i)
        {
            ref var eff = ref aeh->IncomingEffects[i];
            ref readonly var prev = ref act.IncomingEffects[i];

            var seq = eff.GlobalSequence;
            var targetIndex = seq != 0 ? eff.TargetIndex : 0;
            if (seq == 0)
            {
                if (prev.GlobalSequence == 0)
                    continue;
            }
            else if (prev.GlobalSequence == seq && prev.TargetIndex == targetIndex)
            {
                continue;
            }

            // an Effect slot IS the eight raw bytes the game sent; reinterpreted rather than field-copied so
            // a slot Minerva does not decode yet still round-trips through a recording intact
            var slots = eff.Effects.Effects;
            var effects = new ulong[ActorCastEvent.Target.MaxEffects];
            for (var j = 0; j < effects.Length && j < slots.Length; ++j)
                effects[j] = System.Runtime.CompilerServices.Unsafe.As<FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler.Effect, ulong>(ref slots[j]);

            this.ws.Execute(new ActorState.OpIncomingEffect(act.InstanceID, i,
                new ActorIncomingEffect(seq, targetIndex, eff.Source, new ActionID((ActionType)eff.ActionType, eff.ActionId), effects)));
        }
    }

    private void UpdateStatuses(Actor act, IBattleChara chr, nint addr)
    {
        // gate on the CS null-check: Dalamud's StatusList.Length derefs the status-manager block without
        // guarding it, and it's null on some special BattleChara-typed objects (e.g. 0xFF-prefixed) -> NRE
        if (!GameData.HasStatusManager(addr))
            return;

        var list = chr.StatusList;
        var count = Math.Min(list.Length, Actor.NumStatuses);
        for (var i = 0; i < count; ++i)
        {
            var s = list[i];
            ActorStatus cur = default;
            if (s != null && s.StatusId != 0)
            {
                var dur = Math.Min(MathF.Abs(s.RemainingTime), 100000f);
                var source = SanitizeId(s.SourceObject?.GameObjectId ?? 0);
                cur = new ActorStatus(s.StatusId, (ushort)s.Param, this.ws.CurrentTime.AddSeconds(dur), source);
            }
            this.UpdateStatusSlot(act, i, cur);
        }
    }

    private void UpdateStatusSlot(Actor act, int index, ActorStatus value)
    {
        ref readonly var prev = ref act.Statuses[index];
        if (prev.ID == value.ID && prev.SourceID == value.SourceID && prev.Extra == value.Extra
            && (value.ExpireAt - prev.ExpireAt).TotalSeconds is <= 1d and >= -1d)
        {
            act.Statuses[index].ExpireAt = value.ExpireAt;
            return;
        }
        this.ws.Execute(new ActorState.OpStatus(act.InstanceID, index, value));
    }

    // ---------------------------------------------------------------------
    // packet detours: run Original, then queue an op for the next frame
    // (FFXIV processes packets on the main thread, so plain lists are safe)
    // ---------------------------------------------------------------------

    // ActorControl category ids (from BMR's ServerIPC.ActorControlCategory)
    private const uint CatTargetIcon = 34, CatTether = 35, CatTetherCancel = 47, CatModelState = 63, CatDirectorUpdate = 109, CatTargetVFX = 184, CatPlayActionTimeline = 407, CatEObjSetState = 409, CatEObjAnimation = 413;

    /// <summary>
    /// A resolved action: the server has told us the cast landed, on whom, and what it did. Emits the
    /// <see cref="ActorState.OpCastEvent"/> that drives <c>OnEventCast</c> and every cast counter.
    /// </summary>
    private void ActionEffectDetour(uint casterID, Character* casterObj, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targets)
    {
        this.actionEffectHook!.Original(casterID, casterObj, targetPos, header, effects, targets);
        try
        {
            var ev = new ActorCastEvent(
                ActionID.MakeSpell(header->ActionId),
                header->AnimationTargetId,
                // RotationInt is a ushort over the full turn with its ZERO AT -PI, so the -MathF.PI is not
                // cosmetic: without it every cast event reports a heading 180 degrees from the cast that
                // produced it. Measured against a capture of Accept No Imitators -- all 18 Supercell cones,
                // three casters, both actions, delta exactly +180.00. GenericRotatingAOE matches a resolving
                // cast back to its sequence on rotation within 0.05 rad, so nothing ever matched, no sequence
                // ever advanced, and the cones accumulated until the arena had no safe spot left.
                // BossmodReborn: PacketDecoder.IntToFloatAngle(ushort) => (rot * Inv65kDoublePI - PI).Radians()
                new Angle(header->RotationInt * (180f / 32768f) * Angle.DegToRad - MathF.PI),
                *targetPos,
                header->GlobalSequence);

            var raw = (ulong*)effects;
            var numTargets = Math.Min((int)header->NumTargets, 16); // defensive: the packet is fixed-size
            for (var i = 0; i < numTargets; ++i)
            {
                var slots = new ulong[ActorCastEvent.Target.MaxEffects];
                for (var j = 0; j < slots.Length; ++j)
                    slots[j] = raw[i * 8 + j];
                ev.Targets.Add(new ActorCastEvent.Target(targets[i], slots));
            }

            this.QueueActorOp(casterID, new ActorState.OpCastEvent(casterID, ev));
        }
        catch (Exception ex)
        {
            // never let a decode fault take the game's action pipeline down
            Service.Log.Error(ex, "Minerva: ActionEffect decode failed.");
        }
    }

    /// <summary>
    /// A cast started: record where it is aimed before handing the packet on. The coordinates arrive as
    /// unsigned 16-bit fixed point spanning ±1000 yalms, the game's standard position encoding.
    /// </summary>
    private void ActorCastDetour(uint casterID, ActorCastPacket* packet)
    {
        try
        {
            if (packet != null)
                this.castPositions[casterID] = FixedToWorld(packet->PosX, packet->PosY, packet->PosZ);
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Minerva: ActorCast detour failed; continuing without a cast position.");
        }

        this.actorCastHook!.Original(casterID, packet);
    }

    private const float FixedToYalms = 2000f / 65535f;

    private static Vector3 FixedToWorld(ushort x, ushort y, ushort z)
        => new((x * FixedToYalms) - 1000f, (y * FixedToYalms) - 1000f, (z * FixedToYalms) - 1000f);

    private void ActorControlDetour(uint actorID, uint category, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetID, byte replaying)
    {
        this.actorControlHook!.Original(actorID, category, p1, p2, p3, p4, p5, p6, p7, p8, targetID, replaying);
        switch (category)
        {
            case CatTargetIcon: // p1 = marker id (IDScramble.Delta is 0 for us), p2 = target
                this.QueueActorOp(actorID, new ActorState.OpIcon(actorID, p1, p2));
                break;
            case CatTargetVFX:
                this.QueueActorOp(actorID, new ActorState.OpVFX(actorID, p1, p2));
                break;
            case CatTether:
                this.QueueActorOp(actorID, new ActorState.OpTether(actorID, new ActorTetherInfo(p2, p3)));
                break;
            case CatTetherCancel:
                this.QueueActorOp(actorID, new ActorState.OpTether(actorID, default));
                break;
            case CatModelState: // p1 = model state row index
                this.QueueActorOp(actorID, new ActorState.OpModelState(actorID, (byte)p1));
                break;
            case CatPlayActionTimeline: // p1 = timeline id
                this.QueueActorOp(actorID, new ActorState.OpActionTimeline(actorID, (ushort)p1));
                break;
            case CatEObjSetState: // p1 = event-object state
                this.QueueActorOp(actorID, new ActorState.OpActorEState(actorID, (ushort)p1));
                break;
            case CatEObjAnimation:
                // (p1 << 16) | p2, matching BossmodReborn's ordering. This was the other way round, which
                // silently broke every ported module that compares the state against a BMR constant -- 48 of
                // them, including Pallmagia's Roulette, which tests for 0x00040010/0x00040020 and was seeing
                // 0x00100004/0x00200004. Nothing errors when the halves are swapped; the mechanic simply
                // never draws, which is the worst way for it to be wrong.
                this.QueueActorOp(actorID, new ActorState.OpActorEAnim(actorID, ((uint)p1 << 16) | p2));
                break;
            case CatDirectorUpdate:
                this.globalOps.Add(new WorldState.OpDirectorUpdate(p1, p2, p3, p4, p5, p6));
                break;
        }
    }

    private void MapEffectDetour(nint self, uint index, ushort s1, ushort s2)
    {
        this.mapEffectHook!.Original(self, index, s1, s2);
        this.globalOps.Add(new WorldState.OpMapEffect((byte)index, s1 | ((uint)s2 << 16)));
    }

    private void RSVDataDetour(byte* packet)
    {
        this.rsvHook!.Original(packet);
        var key = MemoryHelper.ReadStringNullTerminated((nint)(packet + 4));
        var value = MemoryHelper.ReadString((nint)(packet + 0x34), *(int*)packet);
        this.globalOps.Add(new WorldState.OpRSVData(key, value));
    }

    private void QueueActorOp(ulong id, WorldState.Operation op)
    {
        if (!this.actorOps.TryGetValue(id, out var list))
            this.actorOps[id] = list = [];
        list.Add(op);
    }

    private static ulong SanitizeId(ulong id) => id is 0 or 0xE0000000 ? 0 : id;
}
