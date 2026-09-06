// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V23Gorai;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Polygon> octagonsInner = [];
    private readonly List<Polygon> octagonsOuter = [];
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Unenlightenment && Bounds.Radius > 21f)
        {
            var center = Center;
            var shape = new AOEShapeCustom(center, [new Square(center, 22.5f)], [new Square(center, 20f)]);
            _aoe = [new(shape, center, default, Module.CastFinishAt(spell, 0.5d), shapeDistance: shape.Distance(center, default))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x16 && state == 0x00020001u)
        {
            Bounds = new ArenaBoundsSquare(20f);
            _aoe = [];
        }
        else if (index is >= 0x3D and <= 0x40 && state != 0x00020001u)
        {
            var center = index switch
            {
                0x3D => new(731f, -200f),
                0x3E => new(751f, -200f),
                0x3F => new(731f, -180f),
                0x40 => new(751f, -180f),
                _ => (WPos)default
            };
            if (state == 0x000200010u)
            {
                var rotation = 22.5f.Degrees();
                octagonsInner.Add(new(center, 7.2858f, 8, rotation));
                octagonsOuter.Add(new(center, 9.698552f, 8, rotation));
            }
            else
            {
                var count = octagonsInner.Count;
                for (var i = 0; i < count; ++i)
                {
                    if (octagonsInner[i].Center == center)
                    {
                        octagonsInner.RemoveAt(i);
                        octagonsOuter.RemoveAt(i);
                        break;
                    }
                }
            }
            if (octagonsInner.Count == 0)
            {
                Bounds = new ArenaBoundsSquare(20f);
            }
            else
            {
                Bounds = new ArenaBoundsCustom([new Square(Center, 20f)], [.. octagonsOuter], [.. octagonsInner]);
            }
        }
    }
}
