// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A23HeavyArtilleryUnit;

sealed class A23HeavyArtilleryUnitStates : StateMachineBuilder
{
    public A23HeavyArtilleryUnitStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ManeuverVoltArray>()
            .ActivateOnEnter<LowerLaser>()
            .ActivateOnEnter<UpperLaser>()
            .ActivateOnEnter<EnergyBombardment>()
            .ActivateOnEnter<ManeuverHighPoweredLaser>()
            .ActivateOnEnter<UnconventionalVoltage>()
            .ActivateOnEnter<ManeuverImpactCrusherRevolvingLaser>()
            .ActivateOnEnter<R010Laser>()
            .ActivateOnEnter<Energy>()
            .ActivateOnEnter<ChemicalBurn>()
            .ActivateOnEnter<R030Hammer>();
    }
}
