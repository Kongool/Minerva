// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V22Moko;

abstract class V22MokoStates : StateMachineBuilder
{
    public V22MokoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            // Route 1
            .ActivateOnEnter<Unsheathing>()
            .ActivateOnEnter<VeilSever>()
            // Route 2
            .ActivateOnEnter<ScarletAuspice>()
            .ActivateOnEnter<Clearout>()
            .ActivateOnEnter<Explosion>()
            // Route 3
            .ActivateOnEnter<GhastlyGrasp>()
            .ActivateOnEnter<YamaKagura>()
            // Route 4
            .ActivateOnEnter<Spiritflame>()
            .ActivateOnEnter<Spiritflames>()
            // Standard
            .ActivateOnEnter<SpearmanOrdersFast>()
            .ActivateOnEnter<SpearmanOrdersSlow>()
            .ActivateOnEnter<KenkiReleaseMoonlessNight>()
            .ActivateOnEnter<IronRain>()
            .ActivateOnEnter<Giri>()
            .ActivateOnEnter<AzureAuspice>()
            .ActivateOnEnter<BoundlessScarletAzure>()
            .ActivateOnEnter<UpwellFirst>()
            .ActivateOnEnter<UpwellRest>();
    }
}

sealed class V22MokoPath2States(ModuleBase module) : V22MokoStates(module) { }
sealed class V22MokoOtherPathsStates(ModuleBase module) : V22MokoStates(module) { }
