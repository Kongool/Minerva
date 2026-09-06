// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A22Forgall;

class A22ForgallStates : StateMachineBuilder
{
    public A22ForgallStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BrandOfTheFallen>()
            .ActivateOnEnter<MegiddoFlame2>()
            .ActivateOnEnter<DarkEruption2>()
            .ActivateOnEnter<MortalRay>()
            .ActivateOnEnter<Mow>()
            .ActivateOnEnter<TailDrive>();
    }
}
