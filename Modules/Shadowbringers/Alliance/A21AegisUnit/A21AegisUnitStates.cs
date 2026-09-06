// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A21AegisUnit;

sealed class A21AegisUnitStates : StateMachineBuilder
{
    public A21AegisUnitStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ManeuverDiffusionCannon>()
            .ActivateOnEnter<SurfaceLaserAOE>()
            .ActivateOnEnter<SurfaceLaserSpread>()
            .ActivateOnEnter<BeamCannons>()
            .ActivateOnEnter<ColliderCannons>()
            .ActivateOnEnter<RefractionCannons>()
            .ActivateOnEnter<AntiPersonnelLaser>()
            .ActivateOnEnter<FlightPath>()
            .ActivateOnEnter<HighPoweredLaser>()
            .ActivateOnEnter<ManeuverSaturationBombing>()
            .ActivateOnEnter<LifesLastSong>();
    }
}
