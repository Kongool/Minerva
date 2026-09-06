// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Normal.FTMN2SwordDancer;

[SkipLocalsInit]
sealed class SwordDancerStates : StateMachineBuilder
{
    public SwordDancerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SwordStormCast>()
            .ActivateOnEnter<RushShort1>()
            .ActivateOnEnter<RushShort2>()
            .ActivateOnEnter<TurnInner>()
            .ActivateOnEnter<TurnOuter>()
            .ActivateOnEnter<MartialMystique>()
            .ActivateOnEnter<Cyclosword>()
            .ActivateOnEnter<SwordDance>()
            .ActivateOnEnter<Pierce>()
            .ActivateOnEnter<Steelsbreath>()
            .ActivateOnEnter<RushSurgesword>();
    }
}
