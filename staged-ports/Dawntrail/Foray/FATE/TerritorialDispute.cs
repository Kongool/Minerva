// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.TerritorialDispute;

public enum OID : uint
{
    RuinHound = 0x4D5E,
    Helper = 0x233C,
    IcePillar = 0x4D5F, // R2.000, x0 (spawn during fight)
    RuinHound1 = 0x4DA0, // R1.000, x0 (spawn during fight)
    RuinHound2 = 0x4D60, // R1.000, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 50536, // RuinHound->player, no cast, single-target
    IcePillarCast = 49758, // RuinHound->self, 3.0s cast, single-target
    IcePillar = 49770, // 4D5F->self, 3.0s cast, range 4 circle
    RoaringBlizzard = 49765, // RuinHound->self, 5.0s cast, range 50 60-degree cone
    Rush = 49759, // 4D5F->self, 4.0s cast, range 80 width 4 rect
    AgeOfEndlessFrostCast = 49760, // RuinHound->self, 3.0s cast, single-target
    AgeOfEndlessFrost = 49761, // 4DA0->self, 3.0s cast, range 40 60.000-degree cone
    TheStormWithin = 49756, // RuinHound->self, 5.0s cast, range 10 circle
    TheStormWithin1 = 49766, // 4D60->location, no cast, range 10 circle
    TheStormWithout = 49757, // RuinHound->self, 5.0s cast, range 10-40 donut
    TheStormWithout1 = 49767, // 4D60->location, no cast, range ?-40 donut
}

sealed class IcePillar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IcePillar, new AOEShapeCircle(4.0f));
sealed class RoaringBlizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RoaringBlizzard, new AOEShapeCone(50.0f, 30.0f.Degrees()));
sealed class Rush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rush, new AOEShapeRect(80.0f, 2.0f));
sealed class AgeOfEndlessFrost(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AgeOfEndlessFrost, new AOEShapeCone(40.0f, 30.0f.Degrees()));
sealed class TheStormWithin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheStormWithin, new AOEShapeCircle(10.0f));
sealed class TheStormWithout(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheStormWithout, new AOEShapeDonut(10.0f, 40.0f));

[SkipLocalsInit]
sealed class TerritorialDisputeStates : StateMachineBuilder
{
    public TerritorialDisputeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<IcePillar>()
            .ActivateOnEnter<RoaringBlizzard>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<AgeOfEndlessFrost>()
            .ActivateOnEnter<TheStormWithin>()
            .ActivateOnEnter<TheStormWithout>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, CFCID = 1093u, NameID = 2080u, PrimaryActorOID = (uint)OID.RuinHound, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
public sealed class TerritorialDispute(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
