// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.TheWingedTerror;

public enum OID : uint
{
    Boss = 0x46C1, // R4.68
    Petrifog1 = 0x46C2, // R1.3
    Petrifog2 = 0x4822, // R1.3
    Petrifog3 = 0x4821, // R1.3
    Petrifog4 = 0x4820, // R1.3
    Petrifog5 = 0x481E, // R1.3
    Petrifog6 = 0x481F, // R1.3
    Petrifog7 = 0x481D // R1.3
}

public enum AID : uint
{
    AutoAttack = 42900, // Boss->player, no cast, single-target
    Teleport = 44481, // Boss->location, no cast, single-target

    GaleCannon = 41274, // Boss->self, 5.0s cast, range 40 width 10 rect
    Petrisphere = 41272, // Boss->self, 4.0s cast, single-target
    SphereShatter = 41273 // Petrifog1/Petrifog6/Petrifog5/Petrifog7/Petrifog2/Petrifog3/Petrifog4->self, 2.0s cast, range 7 circle
}

sealed class GaleCannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GaleCannon, new AOEShapeRect(40f, 5f));
sealed class SphereShatter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SphereShatter, 7f);

sealed class TheWingedTerrorStates : StateMachineBuilder
{
    public TheWingedTerrorStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<GaleCannon>()
            .ActivateOnEnter<SphereShatter>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, CFCID = 1018u, NameID = 1965u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class TheWingedTerror(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
