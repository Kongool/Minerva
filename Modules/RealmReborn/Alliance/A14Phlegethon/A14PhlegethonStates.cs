// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A14Phlegethon;

class A14PhlegethonStates : StateMachineBuilder
{
    public A14PhlegethonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MegiddoFlame2>()
            .ActivateOnEnter<MegiddoFlame3>()
            .ActivateOnEnter<MegiddoFlame4>()
            .ActivateOnEnter<MegiddoFlame5>()
            .ActivateOnEnter<MoonfallSlash>()
            .ActivateOnEnter<AncientFlare1>()
            .ActivateOnEnter<VacuumSlash2>();

    }
}
