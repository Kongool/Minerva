// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V24Shishio;

sealed class V24ShishioStates : StateMachineBuilder
{
    public V24ShishioStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            // Route 8
            .ActivateOnEnter<ThunderVortex>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<Vasoconstrictor>()
            // Route 9
            .ActivateOnEnter<FocusedTremorYokiUzu>()
            // Route 10
            .ActivateOnEnter<RightLeftSwipe>()
            // Route 11
            .ActivateOnEnter<Reisho1>()
            .ActivateOnEnter<Reisho2>()
            // Standard
            .ActivateOnEnter<UnsagelySpinYokiThunderOneTwoThreefold>()
            .ActivateOnEnter<NoblePursuit>()
            .ActivateOnEnter<Levinburst>()
            .ActivateOnEnter<Enkyo>()
            .ActivateOnEnter<OnceTwiceThriceRokujo>()
            .ActivateOnEnter<SplittingCry>()
            .ActivateOnEnter<CloudToCloud1>()
            .ActivateOnEnter<CloudToCloud2>()
            .ActivateOnEnter<CloudToCloud3>()
            .ActivateOnEnter<Rokujo>()
        ;
    }
}