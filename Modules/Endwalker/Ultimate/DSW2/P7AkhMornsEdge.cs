// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

// TODO: assignments?
sealed class P7AkhMornsEdge(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.AkhMornsEdgeAOEFirstNormal1 or (uint)AID.AkhMornsEdgeAOEFirstNormal2 or (uint)AID.AkhMornsEdgeAOEFirstTanks)
        {
            Towers.Add(new(spell.LocXZ, 4f, 1, 6));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.AkhMornsEdgeAOEFirstTanks or (uint)AID.AkhMornsEdgeAOERestTanks)
        {
            ++NumCasts;
        }
    }
}
