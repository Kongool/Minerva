// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V15ThorneKnight;

sealed class V15ThorneKnightStates : StateMachineBuilder
{
    public V15ThorneKnightStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SpringToLife>()
            .ActivateOnEnter<BlisteringBlow>()
            .ActivateOnEnter<BlazingBeacon>()
            .ActivateOnEnter<SignalFlare>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<SacredFlay>()
            .ActivateOnEnter<ForeHonor>()
            .ActivateOnEnter<Cogwheel>();
    }
}
