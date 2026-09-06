// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V25Enenra;

sealed class V25EnenraStates : StateMachineBuilder
{
    public V25EnenraStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<PipeCleaner>()
            .ActivateOnEnter<Uplift>()
            .ActivateOnEnter<Snuff>()
            .ActivateOnEnter<Smoldering>()
            .ActivateOnEnter<IntoTheFire>()
            .ActivateOnEnter<FlagrantCombustion>()
            .ActivateOnEnter<SmokeRings>()
            .ActivateOnEnter<ClearingSmoke>()
            .ActivateOnEnter<StringRock>();
    }
}
