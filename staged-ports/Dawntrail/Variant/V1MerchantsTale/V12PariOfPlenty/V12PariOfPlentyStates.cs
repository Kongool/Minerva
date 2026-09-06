// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.VariantCriterion.V1MerchantsTale.V12PariOfPlenty;

class V12PariOfPlentyStates : StateMachineBuilder
{
    public V12PariOfPlentyStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HeatBurst>()
            .ActivateOnEnter<CarpetRide>()
            .ActivateOnEnter<LeftRightFireflight>()
            .ActivateOnEnter<WheelOfFireflight>()
            .ActivateOnEnter<BurningBleam>()
            .ActivateOnEnter<GaleForce>()
            .ActivateOnEnter<PredatorySwoop>()
            .ActivateOnEnter<TranscendentFlight>()
            .ActivateOnEnter<StrongWind>()
            .ActivateOnEnter<ThievesWeaves>()
            .ActivateOnEnter<SpurningFlames>()
            .ActivateOnEnter<ImpassionedSparks>()
            .ActivateOnEnter<ScouringScorn>();
    }
}
