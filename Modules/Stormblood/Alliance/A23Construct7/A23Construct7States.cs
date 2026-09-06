// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A23Construct7;

class A23Construct7States : StateMachineBuilder
{
    public A23Construct7States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Destroy1>()
            .ActivateOnEnter<Destroy2>()
            .ActivateOnEnter<Accelerate>()
            .ActivateOnEnter<Compress1>()
            .ActivateOnEnter<Compress2>()
            .ActivateOnEnter<Pulverize2>()
            .ActivateOnEnter<Dispose1>()
            .ActivateOnEnter<Dispose3>();
    }
}
