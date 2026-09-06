// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A14Naldthal;

sealed class HeatAboveFlamesBelow(ModuleBase module) : Components.GenericAOEs(module)
{
    public List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCircle _shapeOut = new(8f);
    private static readonly AOEShapeDonut _shapeIn = new(8f, 30f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var shape = ShapeForAction(spell.Action.ID);
        if (shape != null)
        {
            _aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        var shape = ShapeForAction(spell.Action.ID);
        if (shape != null)
        {
            _aoes.Clear();
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var shape = ShapeForAction(spell.Action.ID);
        if (shape != null)
        {
            ++NumCasts;
        }
    }

    private static AOEShape? ShapeForAction(uint action) => action switch
    {
        (uint)AID.FlamesOfTheDeadReal => _shapeIn,
        (uint)AID.LivingHeatReal => _shapeOut,
        _ => null
    };
}
