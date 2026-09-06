// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex2ZoraalJa;

sealed class DawnOfAnAgeArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DawnOfAnAge)
        {
            var center = Center;
            var angle = 45f.Degrees();
            var shape = new AOEShapeCustom(center, [new Square(center, 20f, angle)], [new Square(center, 10f, angle)]);
            _aoe = [new(shape, center, default, Module.CastFinishAt(spell, 0.9d), shapeDistance: shape.Distance(center, default))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x0B && state == 0x00200010u)
        {
            Bounds = new ArenaBoundsSquare(10f, 45f.Degrees());
            _aoe = [];
        }
    }
}
