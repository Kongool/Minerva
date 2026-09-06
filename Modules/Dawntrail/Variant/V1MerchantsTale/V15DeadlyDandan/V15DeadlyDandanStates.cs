// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.VariantCriterion.V1MerchantsTale.V15DeadlyDandan;

class V15DeadlyDandanStates : StateMachineBuilder
{
    public V15DeadlyDandanStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MurkyWaters>()
            .ActivateOnEnter<Devour>()
            .ActivateOnEnter<Spit>()
            .ActivateOnEnter<Dropsea>()
            .ActivateOnEnter<AiryBubbles>()
            .ActivateOnEnter<MawOfTheDeep>()
            .ActivateOnEnter<TidalGuillotine>()
            .ActivateOnEnter<UnfathomableHorror>()
            .ActivateOnEnter<SwallowedSea>()
            .ActivateOnEnter<StingingTentacle>();
    }
}
