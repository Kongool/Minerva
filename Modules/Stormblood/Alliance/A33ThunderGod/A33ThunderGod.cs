// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A33ThunderGod;

// Both rings carry their own five-second cast, and the fight fires them in either order: on 2026-09-15 at 186.8s two
// bullseyes started with the circle while a third started with the donut. BossmodReborn draws this as a concentric
// sequence started by the circle only, so a donut-first bullseye left a donut zone that had already fired and never
// cleared -- three of them stood on the radar by 300s, the arena read as forbidden, and the toons walked to the last
// clear spot and stayed there. Two timed AOEs need no sequence: each ring is drawn from its own cast and removed when
// that cast resolves, and the dodge's own staging is what steps from the safe ring into the spent one.
sealed class HallowedBoltCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HallowedBolt1, 15f);
sealed class HallowedBoltDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HallowedBolt2, new AOEShapeDonut(15f, 30f));

[ModuleInfo(CFCID = 636u, NameID = 7899u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")] //7917
public class A33ThunderGod(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-600, -600), arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Circle(new(-612.5f, -578.4f), 10), new Circle(new(-587.5f, -578.4f), 10), new Circle(new(-575, -600), 10), new Circle(new(-587.5f, -621.5f), 10), new Circle(new(-612.5f, -621.5f), 10), new Circle(new(-625, -600), 10), new Donut(new(-600, -600), 20, 27)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.EphemeralKnight));
    }
}
