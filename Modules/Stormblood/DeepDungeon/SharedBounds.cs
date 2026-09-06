// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.DeepDungeon.HeavenOnHigh;

public static class HoHArenas
{
    public static readonly WPos ArenaCenter = new(-300f, -300f);
    public static readonly Rectangle[] Entrance = [new Rectangle(new(-299.839f, -325.43f), 20f, 1.25f)];
}

public abstract class HoHArena1(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(HoHArenas.ArenaCenter, 24.5f * CosPI.Pi48th, 48, 3.75f.Degrees())], [new Rectangle(new(-300f, -325f), 20f, 1.25f)]);
}

public abstract class HoHArena2(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(HoHArenas.ArenaCenter, 24.5f, 48)], HoHArenas.Entrance);
}
