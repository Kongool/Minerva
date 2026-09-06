// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Dungeon.D01Holminster.D011ForgivenDissonance;

public enum OID : uint
{
    Boss = 0x278A, // R4.0
    Orbs = 0x2896, // R1.1
    Helper2 = 0x2A4B, // R3.45
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    ThePathOfLight = 15813, // Boss->self, 4.0s cast, range 40 circle
    BrazenBull = 15817, // Boss->self, 3.0s cast, single-target
    GibbetCage = 15816, // Boss->self, 3.0s cast, range 8 circle
    Thumbscrew = 15814, // Boss->location, 4.8s cast, width 8 rect charge
    HereticsFork = 15822, // Orbs->self, 8.0s cast, range 40 width 6 cross
    LightShot = 15819, // Orbs->self, 3.0s cast, range 40 width 4 rect
    WoodenHorse = 15815, // Boss->self, 5.0s cast, range 40 90-degree cone
    Pillory = 15812 // Boss->player, 5.0s cast, single-target
}

sealed class Thumbscrew(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Thumbscrew, 4f);
sealed class ThePathofLight(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ThePathOfLight);
sealed class GibbetCage(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GibbetCage, 8f);
sealed class HereticsFork(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HereticsFork, new AOEShapeCross(40f, 3f));
sealed class LightShot(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightShot, new AOEShapeRect(40f, 2f));
sealed class WoodenHorse(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WoodenHorse, new AOEShapeCone(40f, 45f.Degrees()));
sealed class Pillory(ModuleBase module) : Components.SingleTargetDelayableCast(module, (uint)AID.Pillory);

sealed class D011ForgivenDissonanceStates : StateMachineBuilder
{
    public D011ForgivenDissonanceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Thumbscrew>()
            .ActivateOnEnter<ThePathofLight>()
            .ActivateOnEnter<GibbetCage>()
            .ActivateOnEnter<HereticsFork>()
            .ActivateOnEnter<LightShot>()
            .ActivateOnEnter<WoodenHorse>()
            .ActivateOnEnter<Pillory>();
    }
}

[ModuleInfo(CFCID = 676u, NameID = 8299u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "legendoficeman, Malediktus (ported from BMR)")]
public sealed class D011ForgivenDissonance(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-15f, 240f), 19.5f, 36)], [new Rectangle(new(-15f, 259.689f), 20f, 1.062f), new Rectangle(new(-15f, 220.127f), 20f, 1.062f)]);
}
