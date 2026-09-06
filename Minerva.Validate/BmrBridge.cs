using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Minerva.Validate;

/// <summary>
/// Drives a BossmodReborn <c>WorldState</c> from Minerva's recording ops (dual-viewer phase 1). All of
/// BMR is reached by reflection — see <see cref="BmrHost"/> for why.
/// <para>
/// Minerva's op model is a clean rebuild of BMR's, so most ops map 1:1. Ops with no BMR equivalent (or
/// no mapping written yet) are counted in <see cref="Unmapped"/> rather than dropped silently: a missing
/// tether or status is a BMR module that never activates, which would otherwise look like a genuine
/// disagreement instead of a bridge gap.
/// </para>
/// </summary>
internal sealed class BmrBridge
{
    private readonly Assembly asm;
    private readonly object worldState;
    private readonly MethodInfo execute;
    private readonly MethodInfo findActor;
    private readonly Type frameStateType;
    private readonly Type angleType;
    private readonly Type gaugeType;
    private readonly Type actorHpMpType;

    // op types, resolved once
    private readonly Type opFrameStart;
    private readonly Type opCreate;
    private readonly Type opCombat;
    private readonly Type opTargetable;
    private readonly Type opMove;
    private readonly Type opCastInfo;
    private readonly Type opDead;
    private readonly Type opDestroy;
    private readonly Type opTarget;
    private readonly Type opTether;
    private readonly Type opStatus;
    private readonly Type opIcon;
    private readonly Type opCastEvent;
    private readonly Type opZoneChange;
    private readonly Type opPartyModify;
    private readonly Type opHPMP;
    private readonly Type opEAnim;
    private readonly Type opModelState;
    private readonly Type modelStateType;
    private readonly Type opDirectorUpdate;
    private readonly Type opEventState;
    private readonly Type opRenderflags;
    private readonly Type opMapEffect;
    private readonly Type partyMemberType;

    /// <summary>Minerva op tag -> how many we could not translate.</summary>
    public readonly Dictionary<string, int> Unmapped = [];

    public object WorldState => this.worldState;
    public int Applied { get; private set; }

    public BmrBridge(Assembly asm, ulong qpf, string gameVersion)
    {
        this.asm = asm;

        var wsType = Req("BossMod.WorldState");
        this.frameStateType = Req("BossMod.FrameState");
        this.angleType = Req("BossMod.Angle");
        this.gaugeType = Req("BossMod.ClientState+Gauge");
        this.actorHpMpType = Req("BossMod.ActorHPMP");

        this.opFrameStart = Req("BossMod.WorldState+OpFrameStart");
        this.opCreate = Req("BossMod.ActorState+OpCreate");
        this.opCombat = Req("BossMod.ActorState+OpCombat");
        this.opTargetable = Req("BossMod.ActorState+OpTargetable");
        this.opMove = Req("BossMod.ActorState+OpMove");
        this.opCastInfo = Req("BossMod.ActorState+OpCastInfo");
        this.opDead = Req("BossMod.ActorState+OpDead");
        this.opDestroy = Req("BossMod.ActorState+OpDestroy");
        this.opTarget = Req("BossMod.ActorState+OpTarget");
        this.opTether = Req("BossMod.ActorState+OpTether");
        this.opStatus = Req("BossMod.ActorState+OpStatus");
        this.opIcon = Req("BossMod.ActorState+OpIcon");
        this.opCastEvent = Req("BossMod.ActorState+OpCastEvent");
        this.opZoneChange = Req("BossMod.WorldState+OpZoneChange");
        this.opPartyModify = Req("BossMod.PartyState+OpModify");
        this.opHPMP = Req("BossMod.ActorState+OpHPMP");
        this.opEAnim = Req("BossMod.ActorState+OpEventObjectAnimation");
        this.opModelState = Req("BossMod.ActorState+OpModelState");
        this.modelStateType = Req("BossMod.ActorModelState");
        this.opDirectorUpdate = Req("BossMod.WorldState+OpDirectorUpdate");
        this.opEventState = Req("BossMod.ActorState+OpEventState");
        this.opRenderflags = Req("BossMod.ActorState+OpRenderflags");
        this.opMapEffect = Req("BossMod.WorldState+OpMapEffect");
        this.partyMemberType = Req("BossMod.PartyState+Member");

        this.worldState = Activator.CreateInstance(wsType, qpf, gameVersion)
            ?? throw new InvalidOperationException("could not construct BossMod.WorldState");
        this.execute = wsType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance)!;

        var actors = wsType.GetProperty("Actors")?.GetValue(this.worldState)
            ?? wsType.GetField("Actors")?.GetValue(this.worldState)
            ?? throw new InvalidOperationException("BossMod.WorldState.Actors not found");
        this.findActor = actors.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance)!;
        this.actorsOwner = actors;

        Type Req(string name) => asm.GetType(name)
            ?? throw new InvalidOperationException($"{name} not found — BMR's layout has changed.");
    }

    private readonly object actorsOwner;

    public object? FindActor(ulong instanceID) => this.findActor.Invoke(this.actorsOwner, [instanceID]);

    public DateTime CurrentTime =>
        (DateTime)this.worldState.GetType().GetProperty("CurrentTime")!.GetValue(this.worldState)!;

    private void Exec(object op)
    {
        this.execute.Invoke(this.worldState, [op]);
        this.Applied++;
    }

    private object Default(Type t) => Activator.CreateInstance(t)!;

    /// <summary>Advance BMR's clock. Everything time-based (state machines, activations) hangs off this.</summary>
    public void FrameStart(DateTime timestamp, uint index, float dt)
    {
        var frame = Activator.CreateInstance(this.frameStateType, timestamp, (ulong)index * 10_000_000UL, index, dt, dt, 1f)!;
        Exec(Activator.CreateInstance(this.opFrameStart, frame, TimeSpan.FromSeconds(dt), this.Default(this.gaugeType), this.Default(this.angleType))!);
    }

    public void Create(ulong instanceID, uint oid, string name, uint nameID, int actorType, Vector4 posRot, float hitboxRadius,
        uint curHP, uint maxHP, uint shield, uint curMP, uint maxMP, bool isTargetable, bool isAlly, ulong ownerID)
    {
        var typeEnum = Enum.ToObject(Req("BossMod.ActorType"), actorType);
        var classEnum = Enum.ToObject(Req("BossMod.Class"), 0);
        // real HP from the first frame: a boss created at 0/0 is "dead" to BMR's DeathPhase until its first HP op
        var hpmp = Activator.CreateInstance(this.actorHpMpType, curHP, maxHP, shield, curMP, maxMP)!;
        Exec(Activator.CreateInstance(this.opCreate,
            instanceID, oid, /*spawnIndex*/ -1, /*layoutID*/ 0u, name, nameID, typeEnum, classEnum, /*level*/ 90,
            posRot, hitboxRadius, hpmp, isTargetable, isAlly, ownerID, /*fateID*/ 0u, /*renderflags*/ 0)!);

        Type Req(string n) => this.asm.GetType(n)!;
    }

    public void Combat(ulong instanceID, bool value) => Exec(Activator.CreateInstance(this.opCombat, instanceID, value)!);
    public void Targetable(ulong instanceID, bool value) => Exec(Activator.CreateInstance(this.opTargetable, instanceID, value)!);
    public void Move(ulong instanceID, Vector4 posRot) => Exec(Activator.CreateInstance(this.opMove, instanceID, posRot)!);
    public void Dead(ulong instanceID, bool value) => Exec(Activator.CreateInstance(this.opDead, instanceID, value)!);

    // HP has to cross: BMR's DeathPhase -- every TrivialPhase module -- ends the moment the boss reads 0 HP,
    // and an actor created with default HPMP reads exactly that. Without this the phase exited on the frame
    // it started and components never activated.
    public void HPMP(ulong instanceID, uint curHP, uint maxHP, uint shield, uint curMP, uint maxMP)
        => Exec(Activator.CreateInstance(this.opHPMP, instanceID, Activator.CreateInstance(this.actorHpMpType, curHP, maxHP, shield, curMP, maxMP)!)!);

    // The event ops modules key on. Minerva packs an EObj animation as one uint (param1 << 16 | param2), which
    // is exactly what BMR's OnActorEAnim receives, so the split here is the inverse of BMR's own packing.
    public void EAnim(ulong instanceID, uint state)
        => Exec(Activator.CreateInstance(this.opEAnim, instanceID, (ushort)(state >> 16), (ushort)(state & 0xFFFF))!);
    public void ModelState(ulong instanceID, byte modelState)
        => Exec(Activator.CreateInstance(this.opModelState, instanceID, Activator.CreateInstance(this.modelStateType, modelState, (byte)0, (byte)0)!)!);
    public void DirectorUpdate(uint directorID, uint updateID, uint p1, uint p2, uint p3, uint p4)
        => Exec(Activator.CreateInstance(this.opDirectorUpdate, directorID, updateID, p1, p2, p3, p4)!);
    public void EventState(ulong instanceID, byte value) => Exec(Activator.CreateInstance(this.opEventState, instanceID, value)!);
    public void Renderflags(ulong instanceID, int value) => Exec(Activator.CreateInstance(this.opRenderflags, instanceID, value)!);
    public void MapEffect(byte index, uint state) => Exec(Activator.CreateInstance(this.opMapEffect, index, state)!);

    /// <summary>Start a cast. Pass null to clear (cast finished).</summary>
    public void CastInfo(ulong instanceID, uint actionID, ulong targetID, Vector3 location, float total, float elapsed, float rotationRad)
    {
        var castType = this.asm.GetType("BossMod.ActorCastInfo")!;
        var cast = Activator.CreateInstance(castType)!;
        SetMember(cast, "Action", MakeSpell(actionID));
        SetMember(cast, "TargetID", targetID);
        SetMember(cast, "Location", location);
        SetMember(cast, "TotalTime", total);
        SetMember(cast, "ElapsedTime", elapsed);
        SetMember(cast, "Rotation", Activator.CreateInstance(this.angleType, rotationRad)!);
        Exec(Activator.CreateInstance(this.opCastInfo, instanceID, cast)!);
    }

    public void CastClear(ulong instanceID) => Exec(Activator.CreateInstance(this.opCastInfo, instanceID, null)!);

    private object MakeSpell(uint actionID)
    {
        var actionIdType = this.asm.GetType("BossMod.ActionID")!;
        var actionTypeEnum = this.asm.GetType("BossMod.ActionType")!;
        // ActionID(ActionType type, uint id); Spell == 2 in BMR's enum, but resolve by name to be safe
        var spell = Enum.Parse(actionTypeEnum, "Spell");
        return Activator.CreateInstance(actionIdType, spell, actionID)!;
    }

    private static void SetMember(object target, string name, object? value)
    {
        var t = target.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (f != null) { f.SetValue(target, value); return; }
        t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);
    }

    public void Destroy(ulong instanceID) => Exec(Activator.CreateInstance(this.opDestroy, instanceID)!);
    public void Target(ulong instanceID, ulong value) => Exec(Activator.CreateInstance(this.opTarget, instanceID, value)!);
    public void Icon(ulong instanceID, uint iconID, ulong targetID) => Exec(Activator.CreateInstance(this.opIcon, instanceID, iconID, targetID)!);

    public void Tether(ulong instanceID, uint tetherID, ulong target)
    {
        var t = this.asm.GetType("BossMod.ActorTetherInfo")!;
        Exec(Activator.CreateInstance(this.opTether, instanceID, Activator.CreateInstance(t, tetherID, target)!)!);
    }

    public void Status(ulong instanceID, int index, uint statusID, ushort extra, DateTime expireAt, ulong sourceID)
    {
        var t = this.asm.GetType("BossMod.ActorStatus")!;
        Exec(Activator.CreateInstance(this.opStatus, instanceID, index, Activator.CreateInstance(t, statusID, extra, expireAt, sourceID)!)!);
    }

    /// <summary>
    /// A resolved cast. BMR's event carries per-target damage effects that Minerva does not record, so the
    /// target list is left empty — components keyed on MainTargetID behave identically, but anything that
    /// inspects per-target effects will differ. Recorded as a known bridge limitation.
    /// </summary>
    public void CastEvent(ulong instanceID, uint actionID, ulong mainTargetID, System.Numerics.Vector3 targetPos, uint globalSequence, float rotationRad)
    {
        var t = this.asm.GetType("BossMod.ActorCastEvent")!;
        var ev = Activator.CreateInstance(t, MakeSpell(actionID), mainTargetID, /*animationLock*/ 0f, /*maxTargets*/ 1u,
            targetPos, globalSequence, /*sourceSequence*/ 0u, Activator.CreateInstance(this.angleType, rotationRad)!)!;
        Exec(Activator.CreateInstance(this.opCastEvent, instanceID, ev)!);
    }

    public void ZoneChange(ushort zone, ushort cfcID) => Exec(Activator.CreateInstance(this.opZoneChange, zone, cfcID)!);

    // BMR keeps the local player in party slot 0 -- Raid.Player() is that slot, and every critical
    // engagement's CheckPull dereferences it. Minerva records the party list as the game orders it, with
    // the POV wherever it happens to sit, so the POV's slot and slot 0 are swapped on the way through,
    // whichever order the ops arrive in. Without this the party never reached BMR at all, its modules
    // threw at pull, and the comparison reported agreement between Minerva and nothing.
    private int povSlot = -1;
    private (ulong ContentID, ulong InstanceID)? displaced; // whoever Minerva had at slot 0, until the POV's slot is known

    public void PartyModify(int slot, ulong contentID, ulong instanceID, ulong pov)
    {
        var target = slot;
        if (pov != 0)
        {
            if (instanceID == pov)
            {
                this.povSlot = slot;
                target = 0;
                if (this.displaced is { } d && slot != 0)
                {
                    this.EmitParty(slot, d.ContentID, d.InstanceID);
                    this.displaced = null;
                }
            }
            else if (slot == 0)
            {
                if (this.povSlot > 0)
                    target = this.povSlot;
                else
                    this.displaced = (contentID, instanceID);
            }
            else if (slot == this.povSlot)
            {
                target = 0; // the POV left its slot; BMR's slot 0 follows it
                this.povSlot = -1;
            }
        }

        this.EmitParty(target, contentID, instanceID);
    }

    private void EmitParty(int slot, ulong contentID, ulong instanceID)
    {
        var member = Activator.CreateInstance(this.partyMemberType, contentID, instanceID, /*inCutscene*/ false)!;
        Exec(Activator.CreateInstance(this.opPartyModify, slot, member)!);
    }

    public void NoteUnmapped(string tag)
        => this.Unmapped[tag] = this.Unmapped.GetValueOrDefault(tag) + 1;
}
