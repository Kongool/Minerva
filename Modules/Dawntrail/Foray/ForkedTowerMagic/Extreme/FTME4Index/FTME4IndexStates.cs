// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Extreme.FTME4Index;

[SkipLocalsInit]
sealed class FTME4IndexStates : StateMachineBuilder
{
    public FTME4IndexStates(ModuleBase module) : base(module)
    {
        TrivialPhase();
    }
}
