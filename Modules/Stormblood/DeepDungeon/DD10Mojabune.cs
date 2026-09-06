// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh.DD10Mojabune;

public enum OID : uint
{
    Boss = 0x23E1, // R2.4
    CloudHelper = 0x1E8FB8 // R2.0
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target

    AmorphousApplause = 11876, // Boss->self, 5.0s cast, range 25+R 180-degree cone
    ConcussiveOscillation = 11878, // Boss->location, 4.0s cast, range 7 circle
    Overtow = 11877 // Boss->location, 3.0s cast, range 60 circle
}

class ConcussiveOscillationAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ConcussiveOscillation, 7f);
class OvertowKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Overtow, 30f, true, stopAtWall: true);
class AmorphousApplauseAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AmorphousApplause, new AOEShapeCone(27.4f, 90f.Degrees()));

class DD10MojabuneStates : StateMachineBuilder
{
    public DD10MojabuneStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ConcussiveOscillationAOE>()
            .ActivateOnEnter<OvertowKB>()
            .ActivateOnEnter<AmorphousApplauseAOE>();
    }
}

[ModuleInfo(CFCID = 540u, NameID = 7480u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")] // cfcid is 540 -> 549 (personal note for self)
public class DD10Mojabune(WorldState ws, Actor primary) : HoHArena1(ws, primary);

