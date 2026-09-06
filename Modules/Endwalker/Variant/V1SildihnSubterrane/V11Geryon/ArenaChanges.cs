// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V11Geryon;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly List<Square> squares = [];
    private readonly AOEShapeRect square = new(10f, 10f, 10f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ColossalStrike && Bounds.Radius > 21f)
        {
            var center = Center;
            var shape = new AOEShapeCustom(center, [new Square(center, 24.5f)], [new Square(center, 20f)]);
            _aoes.Add(new(shape, center, default, World.FutureTime(4d), shapeDistance: shape.Distance(center, default)));
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x13 && state == 0x00080004u)
        {
            Bounds = new ArenaBoundsSquare(20f);
            _aoes.Clear();
        }
        else if (state == 0x00800040u)
        {
            WPos pos = index switch
            {
                0x05 => new(173f, 167f),
                0x06 => new(193f, 167f),
                0x07 => new(173f, 187f),
                0x08 => new(193f, 187f),
                _ => default
            };
            if (pos != default)
            {
                squares.Add(new Square(pos, 10f));
                _aoes.Add(new(square, pos, default, World.FutureTime(3d)));
            }
        }
        else if (state == 0x08000400u && squares.Count != 0 && index is >= 0x05 and <= 0x08)
        {
            _aoes.Clear();
            var arena = new ArenaBoundsCustom([new Square(Center, 19.5f)], [.. squares]);
            Bounds = arena;
            Center = arena.Center;
            squares.Clear();
        }
        else if (state == 0x00080004u && index is >= 0x05 and <= 0x08)
        {
            Bounds = new ArenaBoundsSquare(19.5f);
            Center = new(183f, 177f);
        }
    }
}
