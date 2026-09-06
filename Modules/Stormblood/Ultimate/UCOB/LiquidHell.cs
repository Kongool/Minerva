// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UCOB;

class LiquidHell(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 6f, (uint)AID.LiquidHell, m => m.Enemies((uint)OID.VoidzoneLiquidHell).Where(z => z.EventState != 7), 1.3f)
{
    public void Reset() => NumCasts = 0;
}

class P1LiquidHell(ModuleBase module) : LiquidHell(module)
{
    public override bool KeepOnPhaseChange => true;
}
