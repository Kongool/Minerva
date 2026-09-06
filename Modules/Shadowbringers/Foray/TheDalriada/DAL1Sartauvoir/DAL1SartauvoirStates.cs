// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL1Sartauvoir;

sealed class DAL1SartauvoirStates : StateMachineBuilder
{
    public DAL1SartauvoirStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PyrokinesisAOE>()
            .ActivateOnEnter<TimeEruption>()
            .ActivateOnEnter<ThermalGustAOE>()
            .ActivateOnEnter<GrandCrossflameAOE>()
            .ActivateOnEnter<Flamedive>()
            .ActivateOnEnter<BurningBlade>()
            .ActivateOnEnter<MannatheihwonFlameRW>()
            .ActivateOnEnter<MannatheihwonFlameRect>()
            .ActivateOnEnter<MannatheihwonFlameCircle>()
            .ActivateOnEnter<Brand>()
            .ActivateOnEnter<Pyroclysm>()
            .ActivateOnEnter<Pyrocrisis>()
            .ActivateOnEnter<Pyrodoxy>();
    }
}
