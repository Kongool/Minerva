// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A24Ealdnarche;

sealed class A24EaldnarcheStates : StateMachineBuilder
{
    public A24EaldnarcheStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<UranosCascade>()
            .ActivateOnEnter<CronosSlingRect>()
            .ActivateOnEnter<CronosSlingCircle>()
            .ActivateOnEnter<CronosSlingDonut>()
            .ActivateOnEnter<EmpyrealVortexOmegaJavelin>()
            .ActivateOnEnter<EmpyrealVortexSpread>()
            .ActivateOnEnter<EmpyrealVortexRW>()
            .ActivateOnEnter<Sleepga>()
            .ActivateOnEnter<Sleep>()
            .ActivateOnEnter<GaeaStream>()
            .ActivateOnEnter<OmegaJavelinSpread>()
            .ActivateOnEnter<Duplicate>()
            .ActivateOnEnter<StellarBurst>()
            .ActivateOnEnter<QuakeFreeze>()
            .ActivateOnEnter<FloodConcentric>()
            .ActivateOnEnter<FloodProximity>()
            .ActivateOnEnter<TornadoFlareBurst>()
            .ActivateOnEnter<Tornado>()
            .ActivateOnEnter<TornadoPull>()
            .ActivateOnEnter<OrbitalLevin>()
            .ActivateOnEnter<Paralysis>()
            .ActivateOnEnter<FlareRect>();
    }
}
