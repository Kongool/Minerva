// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.DeepDungeon.PalaceOfTheDead.DD10PalaceDeathgaze;

public enum OID : uint
{
    Boss = 0x1692 // R6.0
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target

    AeroBlast = 6420, // Boss->self, 3.0s cast, range 40+R circle
    Bombination = 6418, // Boss->self, 3.0s cast, range 6+R circle
    Lumisphere = 6419, // Boss->location, 3.0s cast, range 6 circle
    Stormwind = 6417, // Boss->self, 3.0s cast, range 12+R 90-degree cone
    Whipcrack = 6416 // Boss->player, no cast, single-target
}

class AeroBlast(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AeroBlast);
class Bombination(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Bombination, 12);
class Lumisphere(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Lumisphere, 6);
class Stormwind(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Stormwind, new AOEShapeCone(18f, 45f.Degrees()));

class DD10PalaceDeathgazeStates : StateMachineBuilder
{
    public DD10PalaceDeathgazeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AeroBlast>()
            .ActivateOnEnter<Bombination>()
            .ActivateOnEnter<Lumisphere>()
            .ActivateOnEnter<Stormwind>();
    }
}

[ModuleInfo(CFCID = 174u, NameID = 4986u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD10PalaceDeathgaze(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-300f, -220f), 24.65f, 48)], [new Rectangle(new(-300f, -245f), 20f, 1.25f)]);
}
