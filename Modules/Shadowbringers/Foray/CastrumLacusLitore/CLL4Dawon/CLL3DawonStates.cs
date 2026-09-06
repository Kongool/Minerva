// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CastrumLacusLitore.CLL4Dawon;

sealed class CLL4DawonStates : StateMachineBuilder
{
    public CLL4DawonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<Pentagust>()
            .ActivateOnEnter<FervidPulse>()
            .ActivateOnEnter<FrigidPulse>()
            .ActivateOnEnter<SwoopingFrenzy>()
            .ActivateOnEnter<MoltingPlumage>()
            .ActivateOnEnter<Scratch>()
            .ActivateOnEnter<CrackleHiss>()
            .ActivateOnEnter<RipperClaw>()
            .ActivateOnEnter<SpikeFlail>()
            .ActivateOnEnter<LeftRightHammer>()
            .ActivateOnEnter<VerdantScarletPlume>()
            .ActivateOnEnter<Obey>()
            .ActivateOnEnter<TasteOfBlood>()
            .ActivateOnEnter<NaturesBlood>()
            .ActivateOnEnter<NaturesPulse>()
            .ActivateOnEnter<TwinAgonies>()
            .ActivateOnEnter<TheKingsNotice>()
            .ActivateOnEnter<HeartOfNature>()
            .ActivateOnEnter<WindsPeak>()
            .ActivateOnEnter<WindsPeakKB>();
    }
}
