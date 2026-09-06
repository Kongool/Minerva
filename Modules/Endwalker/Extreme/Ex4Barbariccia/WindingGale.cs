// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex4Barbariccia;

// TODO: not sure how 'spiral arms' are really implemented
class WindingGale(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.WindingGale)
{
    private readonly List<Actor> _casters = [];

    private static readonly AOEShapeDonutSector _shape = new(9f, 11f, 90f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _casters.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var c = _casters[i];
            aoes[i] = new(_shape, c.Position + _shape.OuterRadius * c.Rotation.ToDirection(), c.CastInfo!.Rotation, Module.CastFinishAt(c.CastInfo));
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _casters.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _casters.Remove(caster);
    }
}
