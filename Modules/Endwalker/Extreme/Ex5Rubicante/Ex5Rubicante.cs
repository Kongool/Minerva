// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex5Rubicante;

class ShatteringHeatBoss(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.ShatteringHeatBoss, 4f);
class BlazingRapture(ModuleBase module) : Components.CastCounter(module, (uint)AID.BlazingRaptureAOE);
class InfernoSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.InfernoSpreadAOE, 5f);

[ModuleInfo(CFCID = 924u, NameID = 12057u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex5Rubicante(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(100f, 100f), 20f, 64)]);
}
