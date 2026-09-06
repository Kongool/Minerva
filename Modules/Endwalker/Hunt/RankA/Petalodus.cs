// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Hunt.RankA.Petalodus;

public enum OID : uint
{
    Boss = 0x35FB // R5.400, x1
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    MarineMayhem = 27063, // Boss->self, 5.0s cast, range 40 circle, triple raidwide + damage up
    MarineMayhemAOE1 = 27064, // Boss->self, no cast, range 40 circle, second raidwide hit
    MarineMayhemAOE2 = 27065, // Boss->self, no cast, range 40 circle, third raidwide hit
    Waterga = 27067, // Boss->players, 5.0s cast, range 6 circle, applies magic vulnerability up (2nd hit deadly even for tanks)
    TidalGuillotine = 27068, // Boss->self, 4.0s cast, range 13 circle
    AncientBlizzard = 27069 // Boss->self, 4.0s cast, range 40 45-degree cone
}

class MarineMayhem(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.MarineMayhem, hintExtra: "Raidwide x3");
class Waterga(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Waterga, 6);
class TidalGuillotine(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TidalGuillotine, 13);
class AncientBlizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientBlizzard, new AOEShapeCone(40, 22.5f.Degrees()));

class PetalodusStates : StateMachineBuilder
{
    public PetalodusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MarineMayhem>()
            .ActivateOnEnter<Waterga>()
            .ActivateOnEnter<TidalGuillotine>()
            .ActivateOnEnter<AncientBlizzard>();
    }
}

[ModuleInfo(Group = ModuleGroup.Hunt, CFCID = 0u, NameID = 10632u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Petalodus(WorldState ws, Actor primary) : SimpleBossModule(ws, primary);
