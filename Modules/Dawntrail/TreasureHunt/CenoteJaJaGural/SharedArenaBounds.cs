// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.TreasureHunt.CenoteJaJaGural;

public abstract class SharedBoundsBoss(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaBounds.Center, ArenaBounds)
{
    public static readonly ArenaBoundsCustom ArenaBounds = new([new Polygon(new(0, -372), 19.5f * CosPI.Pi32th, 32)], [new Rectangle(new(0, -352), 20, 1.65f)]);
}
