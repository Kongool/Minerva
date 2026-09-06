// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh.DD20Beccho;

public enum OID : uint
{
    Boss = 0x23E7, // R3.0
    ChokeshinAdds = 0x23E8 // R1.0
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target
    Fragility = 11901, // ChokeshinAdds->self, 3.0s cast, range 8 circle
    NeuroSquama = 11900, // Boss->self, 3.0s cast, range 50 circle, gaze
    Proboscis = 11898, // Boss->player, no cast, single-target
    PsychoSquama = 11899 // Boss->self, 3.0s cast, range 50+R 90-degree cone
}

class PsychoSquamaAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PsychoSquama, new AOEShapeCone(53f, 45f.Degrees()));
class NeuroSquamaLookAway(ModuleBase module) : Components.CastGaze(module, (uint)AID.NeuroSquama);
class FragilityAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Fragility, 8f);

class DD20BecchoStates : StateMachineBuilder
{
    public DD20BecchoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PsychoSquamaAOE>()
            .ActivateOnEnter<NeuroSquamaLookAway>()
            .ActivateOnEnter<FragilityAOE>();
    }
}

[ModuleInfo(CFCID = 541u, NameID = 7481u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD20Beccho(WorldState ws, Actor primary) : HoHArena1(ws, primary);
