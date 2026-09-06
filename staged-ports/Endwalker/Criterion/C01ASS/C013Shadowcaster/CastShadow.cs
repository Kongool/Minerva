// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C013Shadowcaster;

sealed class CastShadow(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCone cone = new(65f, 15f.Degrees());
    public readonly List<AOEInstance> AOEs = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = AOEs.Count;
        if (count == 0)
            return [];
        var max = count > 6 ? 6 : count;
        return CollectionsMarshal.AsSpan(AOEs).Slice(NumCasts, max);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.NCastShadowAOE1 or (uint)AID.SCastShadowAOE1 or (uint)AID.NCastShadowAOE2 or (uint)AID.SCastShadowAOE2)
        {
            AOEs.Add(new(cone, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
            if (AOEs.Count == 12)
                SortHelpers.SortAOEByActivation(AOEs);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (AOEs.Count != 0 && spell.Action.ID is (uint)AID.NCastShadowAOE1 or (uint)AID.SCastShadowAOE1 or (uint)AID.NCastShadowAOE2 or (uint)AID.SCastShadowAOE2)
        {
            ++NumCasts;
        }
    }
}
