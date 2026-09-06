// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Hunt.RankS.ForgivenGossip;

public enum OID : uint
{
    Boss = 0x2A03, // R=0.75
}

public enum AID : uint
{
    AutoAttack = 18129, // Boss->player, no cast, single-target
    Icefall = 17043, // Boss->location, 3.0s cast, range 5 circle, deadly if petrified by gaze
    PetrifyingEye = 18041, // Boss->self, 3.0s cast, range 40 circle
}

class Icefall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Icefall, 5f);
class PetrifyingEye(ModuleBase module) : Components.CastGaze(module, (uint)AID.PetrifyingEye);

class ForgivenGossipStates : StateMachineBuilder
{
    public ForgivenGossipStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PetrifyingEye>()
            .ActivateOnEnter<Icefall>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 8916u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class ForgivenGossip(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
