// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P7SAgdistis;

class ForbiddenFruit3(ModuleBase module) : ForbiddenFruitCommon(module, (uint)AID.StaticMoon)
{
    protected override DateTime? PredictUntetheredCastStart(Actor fruit) => World.FutureTime(10.5d);
}
