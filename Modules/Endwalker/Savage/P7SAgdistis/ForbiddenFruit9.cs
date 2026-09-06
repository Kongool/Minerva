// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P7SAgdistis;

// TODO: implement!
class ForbiddenFruit9(ModuleBase module) : ForbiddenFruitCommon(module, (uint)AID.StymphalianStrike)
{
    protected override DateTime? PredictUntetheredCastStart(Actor fruit) => fruit.OID == (uint)OID.ForbiddenFruitBird ? World.FutureTime(12.5d) : null;
}
