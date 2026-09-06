// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A21ArachneEve;

[SkipLocalsInit]
sealed class A21ArachneEveStates : StateMachineBuilder
{
    public A21ArachneEveStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SpiderWeb>()
            .ActivateOnEnter<DarkSpike>()
            .ActivateOnEnter<SilkenSpray>()
            .ActivateOnEnter<ShadowBurst>()
            .ActivateOnEnter<SpiderThread>()
            .ActivateOnEnter<FrondAffeared>()
            .ActivateOnEnter<TheWidowsEmbrace>()
            .ActivateOnEnter<TheWidowsKiss>()
            .ActivateOnEnter<Pitfall>()
            .ActivateOnEnter<Tremblor>();
    }
}
