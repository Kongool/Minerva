// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Advanced.Ad01MerchantsTale.Ad013LoneSwordmaster;

[SkipLocalsInit]
sealed class LoneSwordmasterStates : StateMachineBuilder
{
    public LoneSwordmasterStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DebuffTracker>()
            .ActivateOnEnter<SteelsbreathRelease>()
            .ActivateOnEnter<MaleficPortent>()
            .ActivateOnEnter<LashOfLight>()
            .ActivateOnEnter<UnyieldingWill>()
            .ActivateOnEnter<HeavensConfluence>()
            .ActivateOnEnter<NearFarFromHeaven>()
            .ActivateOnEnter<WolfsCrossing>()
            .ActivateOnEnter<EchoingHush>()
            .ActivateOnEnter<EchoingHushPuddle>()
            .ActivateOnEnter<EchoingEight>()
            .ActivateOnEnter<StingOfTheScorpion>()
            .ActivateOnEnter<MaleficAlignment>()
            .ActivateOnEnter<WillOfTheUnderworld>()
            .ActivateOnEnter<WillOfTheUnderworld1>()
            .ActivateOnEnter<WaitingWounds>()
            .ActivateOnEnter<SilentEight>()
            .ActivateOnEnter<SteelsbreathReleaseArena>()
            .ActivateOnEnter<ChainTether>();
    }
}
