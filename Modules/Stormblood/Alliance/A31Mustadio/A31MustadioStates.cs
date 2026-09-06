// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A31Mustadio;

class A31MustadioStates : StateMachineBuilder
{
    public A31MustadioStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<EnergyBurst>()
            .ActivateOnEnter<ArmShot>()
            .ActivateOnEnter<LegShot>()
            .ActivateOnEnter<LeftHandgonne>()
            .ActivateOnEnter<RightHandgonne>()
            .ActivateOnEnter<SatelliteBeam>()
            .ActivateOnEnter<Compress>()
            .ActivateOnEnter<BallisticSpread>();
    }
}
