// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C022Gorai;

sealed class TorchingTorment(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private static readonly AOEShapeCircle _shape = new(6);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TorchingTorment && World.Actors.Find(spell.TargetID) is var target && target != null)
            CurrentBaits.Add(new(caster, target, _shape));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NTorchingTormentAOE or (uint)AID.STorchingTormentAOE)
        {
            CurrentBaits.Clear();
            ++NumCasts;
        }
    }
}
