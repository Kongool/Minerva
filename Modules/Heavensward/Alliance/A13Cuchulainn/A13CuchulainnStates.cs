// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A13Cuchulainn;

class A13CuchulainnStates : StateMachineBuilder
{
    public A13CuchulainnStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CorrosiveBile1>()
            .ActivateOnEnter<FlailingTentacles2>()
            //.ActivateOnEnter<FlailingTentacles2Knockback>()
            .ActivateOnEnter<Beckon>()
            .ActivateOnEnter<BileBelow>()
            .ActivateOnEnter<Pestilence>()
            .ActivateOnEnter<BlackLung>()
            .ActivateOnEnter<GrandCorruption>();
    }
}
