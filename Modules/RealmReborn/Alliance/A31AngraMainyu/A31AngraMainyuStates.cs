// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A31AngraMainyu;

class A31AngraMainyuStates : StateMachineBuilder
{
    public A31AngraMainyuStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DoubleVision>()
            .ActivateOnEnter<MortalGaze1>()
            .ActivateOnEnter<Level100Flare1>()
            .ActivateOnEnter<Level150Death1>();
    }
}
