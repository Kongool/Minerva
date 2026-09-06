// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A12IrminsulSawtooth;

class A12IrminsulSawtoothStates : StateMachineBuilder
{
    public A12IrminsulSawtoothStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<WhiteBreath>()
            .ActivateOnEnter<MeanThrash>()
            .ActivateOnEnter<MeanThrashKnockback>()
            .ActivateOnEnter<MucusBomb>()
            .ActivateOnEnter<MucusSpray>()
            .ActivateOnEnter<Rootstorm>()
            .ActivateOnEnter<Ambush>()
            //.ActivateOnEnter<AmbushKnockback>()
            .ActivateOnEnter<ShockwaveStomp>();
    }
}