// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A14Echidna;

class A14EchidnaStates : StateMachineBuilder
{
    public A14EchidnaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SickleStrike>()
            .ActivateOnEnter<SickleSlash1>()
            .ActivateOnEnter<SickleSlash2>()
            .ActivateOnEnter<AbyssalReaper>()
            .ActivateOnEnter<AbyssalReaperKnockback>()
            .ActivateOnEnter<Petrifaction1>()
            .ActivateOnEnter<Petrifaction2>()
            .ActivateOnEnter<Gehenna>()
            .ActivateOnEnter<BloodyHarvest>()
            .ActivateOnEnter<Deathstrike>()
            .ActivateOnEnter<FlameWreath>()
            .ActivateOnEnter<SerpentineStrike>();
    }
}
