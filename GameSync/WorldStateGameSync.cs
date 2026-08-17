using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Memory;
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

    public WorldStateGameSync(WorldState ws)
    {
        this.ws = ws;

        this.actorControlHook = this.TryHook<ActorControlDelegate>("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", this.ActorControlDetour, "ActorControl");
        this.mapEffectHook = this.TryHook<MapEffectDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B FA 41 0F B7 E8", this.MapEffectDetour, "MapEffect");
        this.rsvHook = this.TryHook<RSVDataDelegate>("44 8B 09 4C 8D 41 34", this.RSVDataDetour, "RSVData");
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
            friendly = !chr.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile);
            target = SanitizeId(chr.TargetObjectId);
        }

        var existing = this.ws.Actors.Find(id);
        if (existing == null)
        {
            var type = (ActorType)(((int)obj.ObjectKind << 8) + obj.SubKind);
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

            this.UpdateCast(existing, chr, addr);
            this.UpdateStatuses(existing, chr, addr);
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
            var location = GameData.TryCastLocation(addr, out var loc) ? loc : default;
            cur = new ActorCastInfo
            {
                Action = new ActionID((ActionType)chr.CastActionType, chr.CastActionId),
                TargetID = SanitizeId(chr.CastTargetObjectId),
                Rotation = chr.Rotation.Radians(), // TODO: distinct CS CastRotation for precision
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

        this.ws.Execute(new ActorState.OpCastInfo(act.InstanceID, cur));
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
            case CatEObjAnimation: // p1, p2 = animation params
                this.QueueActorOp(actorID, new ActorState.OpActorEAnim(actorID, p1 | (p2 << 16)));
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
