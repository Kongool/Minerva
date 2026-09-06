// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel3Sartauvoir;

sealed class Duel3SartauvoirStates : StateMachineBuilder
{
    public Duel3SartauvoirStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Flashover>()
            .ActivateOnEnter<FlamingRain>()
            .ActivateOnEnter<TimeEruption>()
            .ActivateOnEnter<Backdraft>()
            .ActivateOnEnter<ThermalGust>()
            .ActivateOnEnter<Flamedive>()
            .ActivateOnEnter<Meltdown>()
            .ActivateOnEnter<SearingWind>()
            .ActivateOnEnter<ThermalWave>()
            .ActivateOnEnter<Pyrolatry>()
            .ActivateOnEnter<PillarOfFlame>()
            .ActivateOnEnter<BioIV>();
    }
}
