// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Raid.M09NVampFatale;

sealed class VampFataleStates : StateMachineBuilder
{
    public VampFataleStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<KillerVoice>()
            .ActivateOnEnter<HalfMoon>()
            .ActivateOnEnter<VampStomp>()
            .ActivateOnEnter<Hardcore>()
            .ActivateOnEnter<Hardcore2>()
            .ActivateOnEnter<FlayingFry>()
            .ActivateOnEnter<CoffinFiller>()
            .ActivateOnEnter<PenetratingPitch>()
            .ActivateOnEnter<BlastBeat>()
            .ActivateOnEnter<DeadWake>()
            .ActivateOnEnter<BrutalRain>()
            .ActivateOnEnter<CrowdKill>()
            .ActivateOnEnter<PulpingPulse>()
            .ActivateOnEnter<AetherlettingHit>()
            .ActivateOnEnter<AetherlettingCross>()
            .ActivateOnEnter<InsatiableThirst>()
            .ActivateOnEnter<Plummet>()
            .ActivateOnEnter<NeckBiter>()
            .ActivateOnEnter<CoffinMaker>();
    }
}
