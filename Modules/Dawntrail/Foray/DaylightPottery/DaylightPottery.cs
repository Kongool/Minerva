// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.DaylightPottery;

public enum OID : uint
{
    CrimsonGremlin = 0x4D8B, // R3.000, x?
    CrescentGremlin = 0x4D8A, // R1.500, x?
    CrescentSapria = 0x4E15, // R1.920, x?
    CrescentDhruva = 0x4E8E, // R2.400, x?
    CrescentOpken = 0x4E13, // R1.690, x?
    CrescentMelia = 0x4E16, // R3.600, x?
    CrescentSoblyn = 0x4E1A, // R2.200, x?
    CrescentBicephalus = 0x4E47, // R2.850, x?
    CrescentBicephalus1 = 0x4E7B, // R2.850, x?
    PersistentPotFate = 0x4D89, // R0.400, x?
    PersistentPot = 0x47CB, // R0.300, x?
    Actor1ea1a1 = 0x1EA1A1, // R0.500, x?, EventObj type
}

public enum AID : uint
{
    AutoAttack = 40542, // 4D8A/4D8B->player, no cast, single-target
    BadMouth = 50224, // 4D8A->player, no cast, single-target
    OffensiveRambling = 50226, // 4D8B->location, 3.0s cast, range 5 circle
    TouchySubject = 50225, // 4D8B->self, 3.0s cast, range 25 width 6 rect
}

sealed class OffensiveRambling(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.OffensiveRambling, 5f);
sealed class TouchySubject(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TouchySubject, new AOEShapeRect(25f, 3f));

[SkipLocalsInit]
sealed class DaylightPotteryStates : StateMachineBuilder
{
    public DaylightPotteryStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<OffensiveRambling>()
            .ActivateOnEnter<TouchySubject>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, GroupID = 1093u, CFCID = 1093u, NameID = 2072u, PrimaryActorOID = (uint)OID.CrimsonGremlin, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = " (ported from BMR)")]
[SkipLocalsInit]
public sealed class DaylightPottery(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
// no singular actor we can use to trigger arena draw
