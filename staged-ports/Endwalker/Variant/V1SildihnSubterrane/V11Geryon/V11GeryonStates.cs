// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V11Geryon;

sealed class V11GeryonStates : StateMachineBuilder
{
    public V11GeryonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<Gigantomill>()
            .ActivateOnEnter<SubterraneanShudderColossalLaunch>()
            .ActivateOnEnter<ColossalStrike>()
            .ActivateOnEnter<ColossalCharge>()
            .ActivateOnEnter<ColossalSlam>()
            .ActivateOnEnter<ColossalSwing>()
            .ActivateOnEnter<RunawaySludge>()
            .ActivateOnEnter<Shockwave>()
            .ActivateOnEnter<Intake>()
            .ActivateOnEnter<RollingBoulder>()
            .ActivateOnEnter<SuddenlySewage>()
            .ActivateOnEnter<RunawayRunoff>();
    }
}
