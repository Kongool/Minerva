// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A35Diabolos;

class A35DiabolosStates : StateMachineBuilder
{
    public A35DiabolosStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Nightmare>()
            .ActivateOnEnter<NightTerror>()
            .ActivateOnEnter<RuinousOmen1>()
            .ActivateOnEnter<RuinousOmen2>()
            .ActivateOnEnter<UltimateTerror>();
    }
}
