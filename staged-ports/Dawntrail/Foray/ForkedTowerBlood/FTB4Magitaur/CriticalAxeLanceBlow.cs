// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB4Magitaur;

sealed class CriticalAxeLanceBlow(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCircle circle = new(20f);
    private readonly AOEShapeDonut donut = new(10, 32f);
    private readonly AOEShapeRect square = new(10f, 10f, 10f);
    private readonly WPos[] squarePositions = FTB4Magitaur.GetSquarePositions();
    private readonly Angle[] squareAngles = FTB4Magitaur.GetSquareAngles();

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.CriticalLanceblowVisual => donut,
            (uint)AID.CriticalAxeblowVisual => circle,
            _ => null
        };
        if (shape != null)
        {
            var act = Module.CastFinishAt(spell, 1.2d);
            if (shape == donut)
            {
                for (var i = 0; i < 3; ++i)
                {
                    AddAOE(square, squarePositions[i], squareAngles[i]);
                }
            }
            else
            {
                var center = Center;
                AddAOE(FTB4Magitaur.GetCircleMinusSquares(center), center);
            }
            AddAOE(shape, spell.LocXZ);
            void AddAOE(AOEShape shape, WPos position, Angle rotation = default) => _aoes.Add(new(shape, position, rotation, act, shapeDistance: shape.Distance(position, rotation)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.CriticalLanceblowDonut or (uint)AID.CriticalLanceblowRect or (uint)AID.CriticalAxeblow1 or (uint)AID.CriticalAxeblow2)
        {
            ++NumCasts;
        }
    }
}
