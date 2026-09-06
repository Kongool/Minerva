// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Quest.Job.BloodOnTheDeck;

public enum OID : uint
{
    Boss = 0x1BED,
    ShamShinobi = 0x1BE8, // R0.5
    AdjunctOstyrgreinHelper = 0x1BEB, // R0.5
    AdjunctOstyrgrein = 0x1BEA, // R0.5
    Vanara = 0x1BE9, // R3.0
    Helper = 0x233C
}

public enum AID : uint
{
    ScytheTail = 8407, // Vanara->self, 5.0s cast, range 4+R circle
    Butcher = 8405, // Vanara->self, 5.0s cast, range 6+R ?-degree cone
    TenkaGoken = 8408, // AdjunctOstyrgrein->self, 5.0s cast, range 8+R 120-degree cone
    Bombslinger1 = 8411, // AdjunctOstyrgreinHelper->location, 3.0s cast, range 6 circle
}

class ScytheTail(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ScytheTail, 7);
class Butcher(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Butcher, new AOEShapeCone(9, 45.Degrees()));
class TenkaGoken(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TenkaGoken, new AOEShapeCone(8.5f, 60.Degrees()));
class Bombslinger(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Bombslinger1, 6);

class GurumiBorlumiStates : StateMachineBuilder
{
    public GurumiBorlumiStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ScytheTail>()
            .ActivateOnEnter<Butcher>()
            .ActivateOnEnter<TenkaGoken>()
            .ActivateOnEnter<Bombslinger>();
    }
}

[ModuleInfo(Group = ModuleGroup.Quest, CFCID = 68098u, NameID = 6289u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class GurumiBorlumi(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, 15.8f), new ArenaBoundsRect(8, 7.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc) => Arena.Actors(World.Actors.Where(x => !x.IsAlly));
}

