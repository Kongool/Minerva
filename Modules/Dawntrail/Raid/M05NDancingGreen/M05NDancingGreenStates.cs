// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M05NDancingGreen;

sealed class M05NDancingGreenStates : StateMachineBuilder
{
    public M05NDancingGreenStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DoTheHustle>()
            .ActivateOnEnter<Spotlight>()
            .ActivateOnEnter<Moonburn>()
            .ActivateOnEnter<DeepCut>()
            .ActivateOnEnter<FullBeat>()
            .ActivateOnEnter<CelebrateGoodTimesDiscoInfernalLetsPose>()
            .ActivateOnEnter<EighthBeats>()
            .ActivateOnEnter<FunkyFloor>()
            .ActivateOnEnter<LetsDance>()
            .ActivateOnEnter<RideTheWaves>()
            .ActivateOnEnter<TwoFourSnapTwist>();
    }
}
