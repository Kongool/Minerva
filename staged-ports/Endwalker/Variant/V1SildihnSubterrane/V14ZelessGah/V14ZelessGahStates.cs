// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V14ZelessGah;

sealed class V14ZelessGahStates : StateMachineBuilder
{
    public V14ZelessGahStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<ArcaneFont>()
            .ActivateOnEnter<InfernGale>()
            .ActivateOnEnter<InfernWellAOE>()
            .ActivateOnEnter<InfernWellPull>()
            .ActivateOnEnter<TrespassersPyre>()
            .ActivateOnEnter<BallOfFire>()
            .ActivateOnEnter<PureFire>()
            .ActivateOnEnter<CastShadow>()
            .ActivateOnEnter<FiresteelFracture>()
            .ActivateOnEnter<ShowOfStrength>();
    }
}
