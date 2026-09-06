// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN2Dahu;

sealed class DRN2DahuStates : StateMachineBuilder
{
    public DRN2DahuStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FallingRock>()
            .ActivateOnEnter<HotCharge>()
            .ActivateOnEnter<Firebreathe>()
            .ActivateOnEnter<HeadDown>()
            .ActivateOnEnter<FirebreatheRotation>()
            .ActivateOnEnter<Shockwave>()
            .ActivateOnEnter<HuntersClaw>()
            .ActivateOnEnter<FeralHowl>()
            .ActivateOnEnter<TailSwing>()
            .ActivateOnEnter<HeatBreath>()
            .ActivateOnEnter<RipperClaw>();
    }
}
