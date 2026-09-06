// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un4Zurvan;

// this is used purely for tracking phase transitions
class P2Eidos(ModuleBase module) : ModuleComponent(module)
{
    public int PhaseIndex { get; private set; }
    public override bool KeepOnPhaseChange => true;

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var nextPhase = (AID)spell.Action.ID switch
        {
            AID.Eidos1 => 1,
            AID.Eidos2 => 2,
            AID.Eidos3 => 3,
            _ => 0
        };
        if (nextPhase > PhaseIndex)
            PhaseIndex = nextPhase;
    }
}
