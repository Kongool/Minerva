// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A31DeathgazeHollow;

class A31DeathgazeHollowStates : StateMachineBuilder
{
    public A31DeathgazeHollowStates(A31DeathgazeHollow module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DarkII>()
            //.ActivateOnEnter<BoltOfDarkness3>()
            .ActivateOnEnter<VoidDeath>()
            .ActivateOnEnter<VoidAeroII>()
            .ActivateOnEnter<VoidBlizzardIIIAOE>()
            .ActivateOnEnter<VoidAeroIVKB1>()
            .ActivateOnEnter<VoidAeroIVKB2>()
            .ActivateOnEnter<Unknown3>()
            .ActivateOnEnter<VoidDeathKB2>()
            .ActivateOnEnter<VoidDeathKB>();
    }
}
