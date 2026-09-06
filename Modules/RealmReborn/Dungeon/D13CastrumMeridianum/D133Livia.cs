// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D13CastrumMeridianum.D133Livia;

public enum OID : uint
{
    Boss = 0x38CE, // R1.507
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target
    Teleport = 28788, // Boss->location, no cast, single-target

    AglaeaClimb = 28798, // Boss->player, 5.0s cast, single-target, tankbuster
    ArtificialPlasma = 28033, // Boss->self, 5.0s cast, range 40 circle, raidwide

    Roundhouse = 28786, // Boss->self, 4.4s cast, single-target, visual
    RoundhouseAOE = 29163, // Helper->self, 5.0s cast, range 10 circle aoe (center)
    RoundhouseDischarge = 28787, // Helper->self, 7.0s cast, range 8 circle aoe (sides)

    InfiniteReach = 28791, // Boss->self, 4.9s cast, single-target, visual
    InfiniteReachSecondaryVisual = 28792, // Boss->self, no cast, single-target, visual
    InfiniteReachDischarge = 28794, // Helper->self, 7.2s cast, range 8 circle aoe
    InfiniteReachAOE = 28793, // Helper->self, 7.2s cast, range 40 width 4 rect aoe
    InfiniteReachAngrySalamander = 28796, // Boss->self, no cast, single-target

    ThermobaricStrike = 29357, // Boss->self, 4.0s cast, single-target, visual
    StunningSweep = 28789, // Boss->self, 3.3s cast, single-target, visual
    StunningSweepDischarge = 28790, // Helper->self, 4.0s cast, range 8 circle aoe
    StunningSweepAOE = 29164, // Helper->self, 4.0s cast, range 8 circle aoe
    ThermobaricCharge = 29356, // Helper->self, 7.0s cast, range 40 circle aoe with 13 (?) falloff
    AngrySalamander = 28795, // Boss->self, 1.5s cast, single-target, visual
    AngrySalamanderAOE = 28797, // Helper->self, 2.5s cast, range 20 width 4 cross

    ArtificialBoost = 29354, // Boss->self, 4.0s cast, single-target, visual (buff)
    ArtificialPlasmaBoostFirst = 29352, // Boss->self, 5.0s cast, raidwide
    ArtificialPlasmaBoostRest = 29353 // Boss->self, no cast, raidwide
}

class AglaeaClimb(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.AglaeaClimb);
class ArtificialPlasma(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ArtificialPlasma);
class AngrySalamanderAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AngrySalamanderAOE, new AOEShapeCross(20f, 2f));
class ThermobaricCharge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThermobaricCharge, 13f)
{
    private readonly StunningSweepDischarge _aoe = module.FindComponent<StunningSweepDischarge>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return _aoe.Casters.Count != 0 ? [] : CollectionsMarshal.AsSpan(Casters);
    }
}
class StunningSweepAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.StunningSweepAOE, 8f);
class StunningSweepDischarge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.StunningSweepDischarge, 8f);
class InfiniteReachDischarge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InfiniteReachDischarge, 8f, 6);
class RoundhouseAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RoundhouseAOE, 10f);
class RoundhouseDischarge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RoundhouseDischarge, 8f)
{
    private readonly RoundhouseAOE _aoe = module.FindComponent<RoundhouseAOE>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return _aoe.Casters.Count != 0 ? [] : CollectionsMarshal.AsSpan(Casters);
    }
}
class InfiniteReachAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InfiniteReachAOE, new AOEShapeRect(40f, 2f));

class D133LiviaStates : StateMachineBuilder
{
    public D133LiviaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AglaeaClimb>()
            .ActivateOnEnter<ArtificialPlasma>()
            .ActivateOnEnter<AngrySalamanderAOE>()
            .ActivateOnEnter<StunningSweepAOE>()
            .ActivateOnEnter<StunningSweepDischarge>()
            .ActivateOnEnter<ThermobaricCharge>()
            .ActivateOnEnter<InfiniteReachDischarge>()
            .ActivateOnEnter<RoundhouseAOE>()
            .ActivateOnEnter<RoundhouseDischarge>()
            .ActivateOnEnter<InfiniteReachAOE>()
        ;
    }
}

[ModuleInfo(CFCID = 15u, NameID = 2118u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D133Livia(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-98f, -33f), 19.5f * CosPI.Pi36th, 36)],
    [new Rectangle(new(-78.187f, -36.886f), 20f, 1.25f, -79f.Degrees())]);
}
