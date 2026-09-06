// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Ultimate.FRU;

// TODO: can target change if boss is provoked mid cast?
sealed class P4AkhMorn(ModuleBase module) : Components.UniformStackSpread(module, 4f, default, 4)
{

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.AkhMornOracle or (uint)AID.AkhMornUsurper && World.Actors.Find(caster.TargetID) is var target && target != null)
            AddStack(target, Module.CastFinishAt(spell, 0.9d));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.AkhMornAOEOracle)
            ++NumCasts;
    }
}
