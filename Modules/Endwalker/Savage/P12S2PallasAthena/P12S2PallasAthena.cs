// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P12S2PallasAthena;

[ModuleInfo(CFCID = 943u, NameID = 12382u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class P12S2PallasAthena(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 95), DefaultBounds)
{
    public static readonly ArenaBoundsRect DefaultBounds = new(20, 15);
    public static readonly ArenaBoundsCircle SmallBounds = new(7);
}
