// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A32Agrias;

class A32AgriasStates : StateMachineBuilder
{
    public A32AgriasStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DivineLight>()
            .ActivateOnEnter<NorthswainsStrikeEphemeralKnight>()
            .ActivateOnEnter<CleansingFlameSpread>()
            .ActivateOnEnter<HallowedBoltAOE>();
    }
}
