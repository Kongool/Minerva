// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.BruteAmbombinatorSharedBounds;

public static class BruteAmbombinatorSharedBounds
{
    public static readonly WPos FirstCenter = new(100f, 100f);
    public static readonly WPos FinalCenter = new(100f, 5f);
    public static readonly ArenaBoundsSquare DefaultArena = new(20f);
    public static readonly ArenaBoundsRect RectArena = new(12.5f, 25f);
    public static readonly ArenaBoundsCustom KnockbackArena = new([new Square(FirstCenter, 20f), new Rectangle(FinalCenter, 12.5f, 25f)]);
}
