// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T01Valigarmanda;

sealed class T01ValigarmandaStates : StateMachineBuilder
{
    public T01ValigarmandaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SlitheringStrike>()
            .ActivateOnEnter<ArcaneLightning>()
            .ActivateOnEnter<StranglingCoilSusurrantBreath>()
            .ActivateOnEnter<SkyruinHailOfFeathersDisasterZoneRuinForetold>()
            .ActivateOnEnter<RuinfallTower>()
            .ActivateOnEnter<RuinfallKB>()
            .ActivateOnEnter<RuinfallAOE>()
            .ActivateOnEnter<ChillingCataclysm>()
            .ActivateOnEnter<NorthernCross>()
            .ActivateOnEnter<FreezingDust>()
            .ActivateOnEnter<CalamitousEcho>()
            .ActivateOnEnter<CalamitousCry1>()
            .ActivateOnEnter<CalamitousCry2>()
            .ActivateOnEnter<Eruption>()
            .ActivateOnEnter<IceTalon>()
            .ActivateOnEnter<Tulidisaster1>()
            .ActivateOnEnter<Tulidisaster2>()
            .ActivateOnEnter<Tulidisaster3>()
            .ActivateOnEnter<ThunderPlatform>()
            .ActivateOnEnter<BlightedBolt1>()
            .ActivateOnEnter<BlightedBolt2>();
    }
}
