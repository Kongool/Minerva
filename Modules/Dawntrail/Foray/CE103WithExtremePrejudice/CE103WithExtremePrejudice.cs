using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.CE103WithExtremePrejudice;

// Occult Crescent Critical Engagement "With Extreme Prejudice". IDs/mechanics referenced from
// BossmodReborn's CE103WithExtremePrejudice. Note: the boss (0x46E1) casts little — the Vassal Vessel
// adds (0x46E2) do the StoneSwell/Rockslide AOEs, and those are placed via TETHERS on the target, not
// at the caster, so they need a hand-written tether component (a generated SimpleAOEs draft gets this wrong).

public enum OID : uint
{
    Boss = 0x46E1,        // 'Command Urn' R4.92 — the CE primary (casts Summon/Assail/AethericBurst/Destruct)
    VassalVessel = 0x46E2, // R2.21 — adds that cast StoneSwell/Rockslide
    CommandUrn = 0x4739,   // R1.0
    Helper = 0x233C,
}

public enum AID : uint
{
    Summon = 41416,        // Boss->self, ~3s, single-target
    Assail = 41417,        // Boss->self, ~3s, single-target
    Destruct = 41422,      // Boss->self, ~3s, single-target
    AethericBurst = 41425, // Helper->self, ~5s, raidwide

    StoneSwell1 = 41420,   // VassalVessel->self, ~1s, range 16 circle (tether 303)
    StoneSwell2 = 39470,   // VassalVessel->self, ~1s, range 16 circle
    Rockslide1 = 41421,    // VassalVessel->self, ~1s, range 40 width 10 cross (tether 304)
    Rockslide2 = 39471,    // VassalVessel->self, ~1s, range 40 width 10 cross
}

public enum TID : uint
{
    StoneSwell = 303, // source tethered to the golem that will erupt in a circle
    Rockslide = 304,  // source tethered to the golem that will erupt in a cross
    StoneSwellSelf = 306, // circle erupts at the tether source itself
}

sealed class AethericBurst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AethericBurst, "Raidwide (mitigate)");

/// <summary>
/// StoneSwell (circle) / Rockslide (cross): telegraphed by a tether, then the tethered target erupts.
/// We draw the AOE at the target when the tether appears and clear it when the matching cast resolves.
/// </summary>
sealed class RockslideStoneSwell(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle Circle = new(16f);
    private static readonly AOEShapeCross Cross = new(40f, 5f);
    private const double Delay = 6d; // ~time from tether to eruption
    private readonly List<AOEInstance> aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(this.aoes);

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        switch (tether.ID)
        {
            case (uint)TID.StoneSwell:
                this.AddOnTarget(source, Circle);
                break;
            case (uint)TID.Rockslide:
                this.AddOnTarget(source, Cross);
                break;
            case (uint)TID.StoneSwellSelf:
                this.aoes.Add(new AOEInstance(Circle, source.Position, default, this.World.FutureTime(Delay), actorID: source.InstanceID));
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID is (uint)AID.StoneSwell1 or (uint)AID.StoneSwell2 or (uint)AID.Rockslide1 or (uint)AID.Rockslide2)
            this.aoes.RemoveAll(a => a.ActorID == caster.InstanceID);
    }

    // the erupting golem is the tether target; it casts the AOE on itself, so key the instance to its id
    private void AddOnTarget(Actor source, AOEShape shape)
    {
        var target = this.World.Actors.Find(source.Tether.Target);
        if (target != null)
            this.aoes.Add(new AOEInstance(shape, target.Position, target.Rotation, this.World.FutureTime(Delay), actorID: target.InstanceID));
    }
}

sealed class CE103WithExtremePrejudiceStates : StateMachineBuilder
{
    public CE103WithExtremePrejudiceStates(ModuleBase module) : base(module)
    {
        this.TrivialPhase()
            .ActivateOnEnter<AethericBurst>()
            .ActivateOnEnter<RockslideStoneSwell>();
    }
}

[ModuleInfo(CFCID = 1018u, NameID = 43u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (ids from BMR CE103)")]
public sealed class CE103WithExtremePrejudice(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-352f, -608f), new ArenaBoundsCircle(19.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        if (!this.PrimaryActor.IsDeadOrDestroyed)
            this.Arena.ActorMarker(this.PrimaryActor.Position, this.PrimaryActor.Rotation, this.PrimaryActor.HitboxRadius, Colors.Enemy);

        foreach (var add in this.Enemies((uint)OID.VassalVessel))
            if (!add.IsDeadOrDestroyed)
                this.Arena.ActorMarker(add.Position, add.Rotation, add.HitboxRadius, Colors.Object);
    }
}
