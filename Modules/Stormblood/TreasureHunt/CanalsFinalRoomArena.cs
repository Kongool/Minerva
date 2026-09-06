// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.TreasureHunt;

public abstract class FinalRoomArena(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly WPos center = new(0, -420);
    private static readonly ArenaBoundsCustom arena = new([new Polygon(center, 19.51f * CosPI.Pi8th, 8, 22.5f.Degrees()), new Rectangle(center, 27.66f, 5.5f),
    new Rectangle(new(0, -440), 5.5f, 7.5f)], [new Rectangle(new(0, -400), 20, 3.35f)]);
}
