// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A36DiabolosHollow;

class A36DiabolosHollowStates : StateMachineBuilder
{
    public A36DiabolosHollowStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
        .ActivateOnEnter<Shadethrust>()
        .ActivateOnEnter<HollowCamisado>()
        .ActivateOnEnter<HollowNightmare>()
        .ActivateOnEnter<HollowOmen1>()
        .ActivateOnEnter<HollowOmen2>()
        .ActivateOnEnter<Blindside>()
        .ActivateOnEnter<Nox>()
        .ActivateOnEnter<HollowNight>()
        .ActivateOnEnter<HollowNightGaze>()
        .ActivateOnEnter<ParticleBeam2>()
        .ActivateOnEnter<ParticleBeam4>();
    }
}
