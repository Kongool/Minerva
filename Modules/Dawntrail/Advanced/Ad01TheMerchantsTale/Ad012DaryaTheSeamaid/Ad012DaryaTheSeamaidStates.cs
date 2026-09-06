// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Advanced.Ad01TheMerchantsTale.Ad012DaryaTheSeamaid;

[SkipLocalsInit]
sealed class DaryaTheSeaMaidStates : StateMachineBuilder
{
    public DaryaTheSeaMaidStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PiercingPlunge>()
            .ActivateOnEnter<SurgingCurrent>()
            .ActivateOnEnter<AquaBall>()
            .ActivateOnEnter<Hydrocannon>()
            .ActivateOnEnter<CeaselessCurrent>()
            .ActivateOnEnter<Hydrofall>()
            .ActivateOnEnter<NearFarTide>()
            .ActivateOnEnter<EchoedSerenade>()
            .ActivateOnEnter<SunkenTreasure>()
            .ActivateOnEnter<Hydrobullet>()
            .ActivateOnEnter<SeaShackles>()
            .ActivateOnEnter<AquaSpear>()
            .ActivateOnEnter<TidalWave>()
            .ActivateOnEnter<HydrobulletSpread>()
            .ActivateOnEnter<TidalSpout>()
            .ActivateOnEnter<AlluringOrder>();
    }
}
