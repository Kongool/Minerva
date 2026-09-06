// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.VariantCriterion.V1MerchantsTale.V11Genie;

class V11GenieStates : StateMachineBuilder
{
    public V11GenieStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FabulousFirecrackersBig>()
            .ActivateOnEnter<FabulousFirecrackersSmall>()
            .ActivateOnEnter<ParadeOfWonders>()
            .ActivateOnEnter<SpectacularSparks>()
            .ActivateOnEnter<Voyage>()
            .ActivateOnEnter<ExplosiveEnding>()
            .ActivateOnEnter<Pyromagicks>()
            .ActivateOnEnter<LampLighting>()
            .ActivateOnEnter<FanningFlame>()
            .ActivateOnEnter<SupernaturalSurprise>()
            .ActivateOnEnter<RubBurn>()
            .ActivateOnEnter<RainbowRoad>()
            .ActivateOnEnter<AetherialBlizzard>()
            .ActivateOnEnter<LampOil>();
    }
}
