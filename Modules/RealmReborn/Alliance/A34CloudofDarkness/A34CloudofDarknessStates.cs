// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A34CloudofDarkness;

class A34CloudofDarknessStates : StateMachineBuilder
{
    public A34CloudofDarknessStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FeintParticleBeam>()
            .ActivateOnEnter<ZeroFormParticleBeam>()
            .ActivateOnEnter<ParticleBeam2>();
    }
}
