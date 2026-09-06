// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V12Silkie;

sealed class V12SilkieStates : StateMachineBuilder
{
    public V12SilkieStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<DustBluster>()
            .ActivateOnEnter<WashOut>()
            .ActivateOnEnter<SilkenPuff>()
            .ActivateOnEnter<BracingDuster>()
            .ActivateOnEnter<ChillingDuster>()
            .ActivateOnEnter<SlipperySoap>()
            .ActivateOnEnter<SpotRemover>()
            .ActivateOnEnter<SqueakyCleanConeSmall>()
            .ActivateOnEnter<SqueakyCleanConeBig>()
            .ActivateOnEnter<PuffAndTumble>()
            .ActivateOnEnter<CarpetBeater>()
            .ActivateOnEnter<EasternEwers>()
            .ActivateOnEnter<TotalWashDustBluster>();
    }
}
