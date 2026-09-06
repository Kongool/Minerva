// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C032Lala;

class SymmetricSurge(ModuleBase module) : Components.UniformStackSpread(module, 6, 0)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.SurgeVector)
            AddStack(actor, status.ExpireAt);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.NSymmetricSurgeAOE or AID.SSymmetricSurgeAOE)
            Stacks.Clear();
    }
}
