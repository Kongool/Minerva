// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A11Mateus;

class A11MateusStates : StateMachineBuilder
{
    public A11MateusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HypothermalCombustion>()
            .ActivateOnEnter<IceSpiral>()
            .ActivateOnEnter<DarkBlizzardIII>()
            .ActivateOnEnter<Chill>()
            .ActivateOnEnter<BlizzardIV>()
            .ActivateOnEnter<IceBubbleBlizzardIIITowers>()
            .ActivateOnEnter<FinRays>()
            .ActivateOnEnter<FlashFreeze>()
            .ActivateOnEnter<Froth>()
            .ActivateOnEnter<Snowpierce>()
            .ActivateOnEnter<BlizzardSphere>();
    }
}
