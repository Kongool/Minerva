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

    public void Create(ulong instanceID, uint oid, string name, uint nameID, int actorType, Vector4 posRot, float hitboxRadius, bool isTargetable, bool isAlly, ulong ownerID)
    {
        var typeEnum = Enum.ToObject(Req("BossMod.ActorType"), actorType);
        var classEnum = Enum.ToObject(Req("BossMod.Class"), 0);
        Exec(Activator.CreateInstance(this.opCreate,
            instanceID, oid, /*spawnIndex*/ -1, /*layoutID*/ 0u, name, nameID, typeEnum, classEnum, /*level*/ 90,
            posRot, hitboxRadius, this.Default(this.actorHpMpType), isTargetable, isAlly, ownerID, /*fateID*/ 0u, /*renderflags*/ 0)!);

        Type Req(string n) => this.asm.GetType(n)!;
    }

    public void Combat(ulong instanceID, bool value) => Exec(Activator.CreateInstance(this.opCombat, instanceID, value)!);
    public void Targetable(ulong instanceID, bool value) => Exec(Activator.CreateInstance(this.opTargetable, instanceID, value)!);
    public void Move(ulong instanceID, Vector4 posRot) => Exec(Activator.CreateInstance(this.opMove, instanceID, posRot)!);
    public void Dead(ulong instanceID, bool value) => Exec(Activator.CreateInstance(this.opDead, instanceID, value)!);

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

    public void NoteUnmapped(string tag)
        => this.Unmapped[tag] = this.Unmapped.GetValueOrDefault(tag) + 1;
}
