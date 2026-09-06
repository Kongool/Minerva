// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M02NHoneyBLovely;

sealed class M02NHoneyBLovelyStates : StateMachineBuilder
{
    public M02NHoneyBLovelyStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<CallMeHoney>()
            .ActivateOnEnter<SweetheartsN>()
            .ActivateOnEnter<TemptingTwist>()
            .ActivateOnEnter<HoneyBeeline>()
            .ActivateOnEnter<HoneyedBreeze>()
            .ActivateOnEnter<HoneyBLive>()
            .ActivateOnEnter<Heartsore>()
            .ActivateOnEnter<Heartsick>()
            .ActivateOnEnter<Fracture>()
            .ActivateOnEnter<Loveseeker>()
            .ActivateOnEnter<BlowKiss>()
            .ActivateOnEnter<HoneyBFinale>()
            .ActivateOnEnter<DropOfVenom>()
            .ActivateOnEnter<SplashOfVenom>()
            .ActivateOnEnter<Splinter>()
            .ActivateOnEnter<BlindingLove1>()
            .ActivateOnEnter<BlindingLove2>()
            .ActivateOnEnter<HeartStruck1>()
            .ActivateOnEnter<HeartStruck2>()
            .ActivateOnEnter<HeartStruck3>();
    }
}
