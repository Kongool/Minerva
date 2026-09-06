// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.AllureOfTheOccult;

public enum OID : uint
{
    SensualSandy = 0x4D56,
    Helper = 0x233C,
    PoisonCloud = 0x4D57, // R1.700, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 50535, // SensualSandy->player, no cast, single-target
    PutridBreath = 48944, // SensualSandy->self, 5.0s cast, range 25 130.000-degree cone
    PutridBreath1 = 48952, // SensualSandy->self, 3.0s cast, range 25 130.000-degree cone
    WildWildBreath = 48945, // SensualSandy->self, 5.0s cast, range 30 width 6 cross
    WildWildWildWildWildBreath = 48946, // SensualSandy->self, 5.0s cast, range 30 width 6 cross
    ExtensibleTendrils = 48947, // SensualSandy->self, 3.0s cast, range 30 width 6 cross
    PoisonPassel = 48951, // SensualSandy->self, 3.0s cast, single-target
    Burst = 48950, // 4D57->self, 5.0s cast, range 10 circle
}

sealed class PutridBreath(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.PutridBreath, (uint)AID.PutridBreath1],
    new AOEShapeCone(25.0f, 65.0f.Degrees()));
sealed class WildWildBreath(ModuleBase module) : Components.SimpleAOEGroups(module,
    [(uint)AID.WildWildBreath, (uint)AID.WildWildWildWildWildBreath, (uint)AID.ExtensibleTendrils], new AOEShapeCross(30.0f, 3.0f));
sealed class Burst(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Burst, 10f);

[SkipLocalsInit]
sealed class AllureOfTheOccultStates : StateMachineBuilder
{
    public AllureOfTheOccultStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PutridBreath>()
            .ActivateOnEnter<WildWildBreath>()
            .ActivateOnEnter<Burst>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, GroupID = 1093u, CFCID = 1093u, NameID = 2078u, PrimaryActorOID = (uint)OID.SensualSandy, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
public sealed class AllureOfTheOccult(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
