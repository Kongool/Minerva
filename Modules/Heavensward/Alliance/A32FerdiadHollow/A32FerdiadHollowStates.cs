// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A32FerdiadHollow;

class A32FerdiadHollowStates : StateMachineBuilder
{
    public A32FerdiadHollowStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Blackbolt>()
            .ActivateOnEnter<Blackfire2>()
            .ActivateOnEnter<JestersJig1>()
            .ActivateOnEnter<JestersReap>()
            .ActivateOnEnter<JestersReward>()
            .ActivateOnEnter<JongleursX>()
            .ActivateOnEnter<JugglingSphere>()
            .ActivateOnEnter<JugglingSphere2>()
            .ActivateOnEnter<AtmosAOE1>()
            .ActivateOnEnter<AtmosAOE2>()
            .ActivateOnEnter<AtmosDonut>()
            .ActivateOnEnter<PetrifyingEye>()
            .ActivateOnEnter<Flameflow1>()
            .ActivateOnEnter<Flameflow2>()
            .ActivateOnEnter<Flameflow3>()
            .ActivateOnEnter<Unknown4>()
            .ActivateOnEnter<Unknown6>();
    }
}
