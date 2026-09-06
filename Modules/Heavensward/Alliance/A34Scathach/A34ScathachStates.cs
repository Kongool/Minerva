// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A34Scathach;

class A34ScathachStates : StateMachineBuilder
{
    public A34ScathachStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            //.ActivateOnEnter<ThirtyCries>()
            .ActivateOnEnter<ThirtyThorns4>()
            .ActivateOnEnter<ThirtySouls>()
            .ActivateOnEnter<ThirtyArrows2>()
            .ActivateOnEnter<ThirtyArrows1>()
            .ActivateOnEnter<TheDragonsVoice>()
            .ActivateOnEnter<Shadespin2>()
            .ActivateOnEnter<Shadesmite1>()
            .ActivateOnEnter<Shadesmite2>()
            .ActivateOnEnter<Shadesmite3>()
            .ActivateOnEnter<Nox>()
            .ActivateOnEnter<MarrowDrain1>()
            .ActivateOnEnter<MarrowDrain2>()
            .ActivateOnEnter<Pitfall>()
            .ActivateOnEnter<FullSwing>()
            .ActivateOnEnter<BigHug>();
    }
}
