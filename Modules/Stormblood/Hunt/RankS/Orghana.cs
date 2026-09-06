// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Hunt.RankS.Orghana;

public enum OID : uint
{
    Boss = 0x1AB3, // R=5.04
}

public enum AID : uint
{
    AutoAttack = 7998, // Boss->player, no cast, single-target
    FlamingEpigraph = 7999, // Boss->location, no cast, range 10 circle, targets random player
    TremblingEpigraph = 8000, // Boss->self, 5.0s cast, range 40 circle, usually raidwide x4
    TremblingEpigraph2 = 8001, // Boss->self, no cast, range 40 circle
    FlaringEpigraph = 8002, // Boss->self, 5.0s cast, range 40 circle
    Epigraph = 7997, // Boss->self, 3.5s cast, range 50+R width 8 rect
}

class TremblingEpigraph(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TremblingEpigraph, "Raidwide x4");
class FlaringEpigraph(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FlaringEpigraph);
class Epigraph(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Epigraph, new AOEShapeRect(55.04f, 4));

class OrghanaStates : StateMachineBuilder
{
    public OrghanaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TremblingEpigraph>()
            .ActivateOnEnter<FlaringEpigraph>()
            .ActivateOnEnter<Epigraph>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 5986u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class Orghana(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
