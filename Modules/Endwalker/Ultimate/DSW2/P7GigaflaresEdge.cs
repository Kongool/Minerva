// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

sealed class P7GigaflaresEdge(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShapeCircle _shape = new(20f); // TODO: verify falloff

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoes.Count != 0 ? CollectionsMarshal.AsSpan(_aoes)[..1] : [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.GigaflaresEdgeAOE1 or (uint)AID.GigaflaresEdgeAOE2 or (uint)AID.GigaflaresEdgeAOE3)
        {
            _aoes.Add(new(_shape, caster.Position, default, Module.CastFinishAt(spell)));
            SortHelpers.SortAOEByActivation(_aoes);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.GigaflaresEdgeAOE1 or (uint)AID.GigaflaresEdgeAOE2 or (uint)AID.GigaflaresEdgeAOE3)
        {
            ++NumCasts;
            if (_aoes.Count != 0)
            {
                _aoes.RemoveAt(0);
            }
        }
    }
}
