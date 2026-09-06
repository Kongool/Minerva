// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A23Amon;

class A23AmonStates : StateMachineBuilder
{
    public A23AmonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BlizzagaForte>()
            .ActivateOnEnter<Darkness>()
            .ActivateOnEnter<CurtainCall>()
            .ActivateOnEnter<ThundagaForte1>()
            .ActivateOnEnter<ThundagaForte2>();
    }
}
