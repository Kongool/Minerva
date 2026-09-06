// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.VariantCriterion.V1MerchantsTale.V13DaryaTheSeamaid;

class V13DaryaTheSeamaidStates : StateMachineBuilder
{
    public V13DaryaTheSeamaidStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PiercingPlunge>()
            .ActivateOnEnter<EchoedSerenade>()
            .ActivateOnEnter<SurgingCurrent>()
            .ActivateOnEnter<Hydrofall>()
            .ActivateOnEnter<SunkenTreasure>()
            .ActivateOnEnter<AquaBall>()
            .ActivateOnEnter<NearFarTide>()
            .ActivateOnEnter<CeaselessCurrent>()
            .ActivateOnEnter<Hydrocannon>()
            .ActivateOnEnter<BigWave>()
            .ActivateOnEnter<AlluringOrder>()
            .ActivateOnEnter<AquaSpear>()
            .ActivateOnEnter<SirenSphere>();
    }
}
