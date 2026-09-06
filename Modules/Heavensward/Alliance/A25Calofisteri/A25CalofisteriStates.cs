// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A25Calofisteri;

class A25CalofisteriStates : StateMachineBuilder
{
    public A25CalofisteriStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AuraBurst>()
            .ActivateOnEnter<DepthCharge>()
            .ActivateOnEnter<Extension2>()
            .ActivateOnEnter<FeintParticleBeam1>()
            .ActivateOnEnter<Penetration>()
            .ActivateOnEnter<Graft>()
            .ActivateOnEnter<Haircut1>()
            .ActivateOnEnter<Haircut2>()
            .ActivateOnEnter<SplitEnd1>()
            .ActivateOnEnter<SplitEnd2>();
    }
}
