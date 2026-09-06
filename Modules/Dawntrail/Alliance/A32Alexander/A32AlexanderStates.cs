// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A32Alexander;

sealed class A32AlexanderStates : StateMachineBuilder
{
    public A32AlexanderStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<BanishgaIV>()
            .ActivateOnEnter<DivineArrowCone>()
            .ActivateOnEnter<DivineArrowCircles>()
            .ActivateOnEnter<DivineArrowLines>()
            .ActivateOnEnter<BanishgaIVSpread>()
            .ActivateOnEnter<HolyII>()
            .ActivateOnEnter<ImpartialRuling>()
            .ActivateOnEnter<RadiantSacrament>()
            .ActivateOnEnter<DivineSpear>()
            .ActivateOnEnter<MegaHoly>()
            .ActivateOnEnter<Activate>()
            .ActivateOnEnter<PerfectDefense>()
            .ActivateOnEnter<HolyFlame>()
            .ActivateOnEnter<Shock>()
            .ActivateOnEnter<CircuitShock>()
            .ActivateOnEnter<DivineJudgment>()
            .ActivateOnEnter<Electrify>()
            .ActivateOnEnter<DivineBolt>();
    }
}
