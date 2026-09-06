// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M04NWickedThunder;

sealed class M04NWickedThunderStates : StateMachineBuilder
{
    public M04NWickedThunderStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<WickedHypercannon>()
            .ActivateOnEnter<WickedJolt>()
            .ActivateOnEnter<WickedBolt>()
            .ActivateOnEnter<WickedCannon>()
            .ActivateOnEnter<WrathOfZeus>()
            .ActivateOnEnter<SidewiseSpark>()
            .ActivateOnEnter<SoaringSoulpress>()
            .ActivateOnEnter<StampedingThunder>()
            .ActivateOnEnter<BewitchingFlight>()
            .ActivateOnEnter<Thunderslam>()
            .ActivateOnEnter<Thunderstorm>()
            .ActivateOnEnter<WitchHunt>();
    }
}
