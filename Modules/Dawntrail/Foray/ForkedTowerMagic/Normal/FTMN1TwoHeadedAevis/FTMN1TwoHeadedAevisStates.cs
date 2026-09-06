// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Normal.FTMN1TwoHeadedAevis;

[SkipLocalsInit]
sealed class TwoHeadedAevisStates : StateMachineBuilder
{
    public TwoHeadedAevisStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Buffet>()
            .ActivateOnEnter<LightningIcePoison>()
            .ActivateOnEnter<StormsBreath>()
            .ActivateOnEnter<ThunderfrostTempest>()
            .ActivateOnEnter<TwoTerrors>()
            .ActivateOnEnter<HypothermalCombustionShock>()
            .ActivateOnEnter<HissingReprise>()
            .ActivateOnEnter<BlazeLoop>()
            .ActivateOnEnter<ArcaneBeacon>()
            .ActivateOnEnter<Archaeofury1>()
            .ActivateOnEnter<Archaeofury2>();
    }
}
