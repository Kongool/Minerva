// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A24Yiazmat;

class A24YiazmatStates : StateMachineBuilder
{
    public A24YiazmatStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<RakeTB>()
            .ActivateOnEnter<RakeSpread>()
            .ActivateOnEnter<RakeAOE>()
            .ActivateOnEnter<RakeLoc1>()
            .ActivateOnEnter<RakeLoc2>()
            .ActivateOnEnter<StoneBreath>()
            .ActivateOnEnter<DustStorm2>()
            .ActivateOnEnter<WhiteBreath>()
            .ActivateOnEnter<AncientAero>()
            .ActivateOnEnter<Karma>()
            .ActivateOnEnter<UnholyDarkness>()
            .ActivateOnEnter<SolarStorm1>();
    }
}
