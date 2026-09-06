// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A22UltimaOmega;

sealed class A22UltimaOmegaStates : StateMachineBuilder
{
    public A22UltimaOmegaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<IonEffluxCitadelBusterHyperPulseChemicalBomb>()
            .ActivateOnEnter<Antimatter>()
            .ActivateOnEnter<OmegaBlaster>()
            .ActivateOnEnter<EnergyRay>()
            .ActivateOnEnter<CitadelSiege>()
            .ActivateOnEnter<CitadelSiegeHint>()
            .ActivateOnEnter<MultiMissileBig>()
            .ActivateOnEnter<MultiMissileSmall>()
            .ActivateOnEnter<Crash>()
            .ActivateOnEnter<TractorBeam>()
            .ActivateOnEnter<AntiPersonnelMissile>()
            .ActivateOnEnter<SurfaceMissile>()
            .ActivateOnEnter<ChemicalBombHyperPulse>()
            .ActivateOnEnter<GuidedMissile>()
            .ActivateOnEnter<TrajectoryProjection>();
    }
}
