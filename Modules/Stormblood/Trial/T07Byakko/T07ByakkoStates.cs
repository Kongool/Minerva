// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T07Byakko;

class T07ByakkoStates : StateMachineBuilder
{
    public T07ByakkoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<StormPulse>()
            .ActivateOnEnter<HeavenlyStrike>()
            .ActivateOnEnter<HeavenlyStrikeSpread>()
            .ActivateOnEnter<SweepTheLeg1>()
            .ActivateOnEnter<SweepTheLeg3>()
            .ActivateOnEnter<TheRoarOfThunder>()
            .ActivateOnEnter<ImperialGuard>()
            //.ActivateOnEnter<HighestStakes>() stack marker not being removed properly
            .ActivateOnEnter<FireAndLightning>()
            .ActivateOnEnter<DistantClap>()
            //.ActivateOnEnter<HundredfoldHavoc>() just not appearing, unsure why
            .ActivateOnEnter<AratamaForce>();
    }
}
