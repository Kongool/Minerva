// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V12Silkie;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TotalWash && Bounds.Radius != 20f)
        {
            var center = Center;
            var shape = new AOEShapeCustom(center, [new Square(center, 29.5f)], [new Square(center, 20f)]);
            _aoe = [new(shape, center, default, World.FutureTime(1.8d), shapeDistance: shape.Distance(center, default))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x04 && state == 0x00020001u)
        {
            Bounds = new ArenaBoundsSquare(20f);
            _aoe = [];
        }
    }
}
