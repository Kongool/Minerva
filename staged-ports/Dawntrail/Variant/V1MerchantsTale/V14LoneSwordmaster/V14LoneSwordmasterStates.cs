// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.VariantCriterion.V1MerchantsTale.V14LoneSwordmaster;

class V14LoneSwordmasterStates : StateMachineBuilder
{
    public V14LoneSwordmasterStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DebuffTracker>()
            .ActivateOnEnter<LashOfLight>()
            .ActivateOnEnter<WillOfTheUnderworld>()
            .ActivateOnEnter<EarthRendingEight>()
            .ActivateOnEnter<EarthRendingEightCross>()
            .ActivateOnEnter<WaitingWounds>()
            .ActivateOnEnter<HeavensConfluence>()
            .ActivateOnEnter<HeavensConfluenceIcon>()
            .ActivateOnEnter<CrusherOfLions>()
            .ActivateOnEnter<UnyieldingWill>()
            .ActivateOnEnter<SteelsbreathRelease>()
            .ActivateOnEnter<SteelsbreathRelease1>()
            .ActivateOnEnter<Concentrativity>()
            .ActivateOnEnter<ConcentrativityKnockback>()
            .ActivateOnEnter<ConcentravityMagnetFloor>()
            .ActivateOnEnter<ConcentrativityRocks>()
            .ActivateOnEnter<MawOfTheWolf>()
            .ActivateOnEnter<StingOfTheScorpion>()
            .ActivateOnEnter<PlummetSmall>()
            .ActivateOnEnter<Plummet>()
            .ActivateOnEnter<PlummetBig>()
            .ActivateOnEnter<PlummetProximity>()
            .ActivateOnEnter<WillOfTheUnderworldRocks>();
    }
}
