// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh.DD50Gozu;

public enum OID : uint
{
    Boss = 0x23E9, // R3.45
    Gloom = 0x23EA, // R1.0
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target

    EyeOfTheFire = 11922, // Boss->self, 3.0s cast, range 40 circle, gaze mechanic
    Visual = 11923, // Boss->self, no cast, single-target, used after EyeOfTheFire
    RustingClaw = 11919, // Boss->self, 3.5s cast, range 8+R 120-degree cone
    TheSpin = 11921, // Boss->self, 7.5s cast, range 40+R circle, proximity AOE, optimal range around 20
    VoidSpark = 11924, // Gloom->self, 2.0s cast, range 7+R circle
    WordsOfWoe = 11920 // Boss->self, 3.0s cast, range 45+R width 6 rect
}

class EyeoftheFire(ModuleBase module) : Components.CastGaze(module, (uint)AID.EyeOfTheFire);
class RustingClaw(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RustingClaw, new AOEShapeCone(11.45f, 60f.Degrees()));
class TheSpin(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheSpin, 20f);
class VoidSpark(ModuleBase module) : Components.Voidzone(module, 8f, GetVoidzones)
{
    private static List<Actor> GetVoidzones(ModuleBase module) => module.Enemies((uint)OID.Gloom);
}
class WordsofWoe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WordsOfWoe, new AOEShapeRect(48.45f, 3f));

class DD50GozuStates : StateMachineBuilder
{
    public DD50GozuStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<EyeoftheFire>()
            .ActivateOnEnter<RustingClaw>()
            .ActivateOnEnter<TheSpin>()
            .ActivateOnEnter<VoidSpark>()
            .ActivateOnEnter<WordsofWoe>();
    }
}

[ModuleInfo(CFCID = 544u, NameID = 7485u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "LegendofIceman (ported from BMR)")]
public class DD50Gozu(WorldState ws, Actor primary) : HoHArena2(ws, primary);
