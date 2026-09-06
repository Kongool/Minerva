// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A23Kamlanaut;

sealed class A23KamlanautStates : StateMachineBuilder
{
    public A23KamlanautStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<ElementalBladeWide>()
            .ActivateOnEnter<ElementalBladeNarrow>()
            .ActivateOnEnter<ElementalResonance>()
            .ActivateOnEnter<SublimeElementsWide>()
            .ActivateOnEnter<SublimeElementsNarrow>()
            .ActivateOnEnter<EmpyrealBanishIII>()
            .ActivateOnEnter<EmpyrealBanishIV>()
            .ActivateOnEnter<LightBladeIllumedEstoc>()
            .ActivateOnEnter<ShieldBash>()
            .ActivateOnEnter<SublimeEstoc>()
            .ActivateOnEnter<GreatWheelCircle>()
            .ActivateOnEnter<GreatWheelCone>()
            .ActivateOnEnter<TranscendentUnion>()
            .ActivateOnEnter<EnspiritedSwordplayShockwave>()
            .ActivateOnEnter<PrincelyBlow>()
            .ActivateOnEnter<PrincelyBlowKB>();
    }
}
