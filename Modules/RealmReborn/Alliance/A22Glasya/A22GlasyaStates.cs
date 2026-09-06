// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A22Glasya;

class A22GlasyaStates : StateMachineBuilder
{
    public A22GlasyaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Aura>()
            .ActivateOnEnter<VileUtterance>();
    }
}
