// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.VariantCriterion.V1MerchantsTale.V10Rukhkh;

class V10RukhkhStates : StateMachineBuilder
{
    public V10RukhkhStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SphereOfSand>()
            .ActivateOnEnter<SandPlume>()
            .ActivateOnEnter<SandBurst>()
            .ActivateOnEnter<SonicHowl>()
            .ActivateOnEnter<DryTyphoon>()
            .ActivateOnEnter<WindborneSeeds>()
            .ActivateOnEnter<StreamingSands>()
            .ActivateOnEnter<BitingScratch>()
            .ActivateOnEnter<BigBurst>()
            .ActivateOnEnter<FallingRock>();
    }
}
