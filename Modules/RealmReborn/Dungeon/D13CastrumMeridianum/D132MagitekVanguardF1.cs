// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D13CastrumMeridianum.D132MagitekVanguardF1;

public enum OID : uint
{
    Boss = 0x38CD, // R4.4
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    ThermobaricStrike = 28778, // Boss->self, 4.0s cast, single-target, visual
    ThermobaricCharge = 28779, // Helper->self, 7.0s cast, range 60 circle aoe with ? falloff
    Hypercharge = 28780, // Boss->self, 4.1s cast, single-target, visual
    HyperchargeInner = 28781, // Helper->self, 5.0s cast, range 10 circle
    HyperchargeOuter = 28782, // Helper->self, 5.0s cast, range 12-30 donut
    TargetedSupport = 28783, // Boss->self, 4.0s cast, single-target, visual
    TargetedSupportAOE = 28784, // Helper->self, 3.0s cast, range 5 circle aoe
    CermetDrill = 28785, // Boss->player, 5.0s cast, single-target tankbuster
    Overcharge = 29146 // Boss->self, 3.0s cast, range 11 120-degree cone aoe
}

class ThermobaricCharge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThermobaricCharge, 20);
class HyperchargeInner(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HyperchargeInner, 10);
class HyperchargeOuter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HyperchargeOuter, new AOEShapeDonut(12, 30));
class TargetedSupport(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TargetedSupportAOE, 5);
class CermetDrill(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.CermetDrill);
class Overcharge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Overcharge, new AOEShapeCone(11, 60.Degrees()));

class D132MagitekVanguardF1States : StateMachineBuilder
{
    public D132MagitekVanguardF1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ThermobaricCharge>()
            .ActivateOnEnter<HyperchargeInner>()
            .ActivateOnEnter<HyperchargeOuter>()
            .ActivateOnEnter<TargetedSupport>()
            .ActivateOnEnter<CermetDrill>()
            .ActivateOnEnter<Overcharge>();
    }
}

[ModuleInfo(CFCID = 15u, NameID = 2116u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D132MagitekVanguardF1(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Square(new(-13, 31), 19.5f, 11.Degrees()), new Rectangle(new(-9.107f, 51.025f), 8, 1.25f, 11.Degrees())],
    [new Rectangle(new(-9.107f, 51.025f), 8, 1.25f, 11.Degrees())]);
}
