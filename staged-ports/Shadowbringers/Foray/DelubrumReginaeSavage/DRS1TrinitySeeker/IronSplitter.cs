// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS1TrinitySeeker;

sealed class IronSplitter(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.IronSplitter)
{
    public readonly List<AOEInstance> AOEs = [];
    private readonly AOEShape[] _shapes = [new AOEShapeCircle(4f), new AOEShapeDonut(8f, 12f), new AOEShapeDonut(16f, 20f),
    new AOEShapeDonut(4f, 8f), new AOEShapeDonut(12f, 16f), new AOEShapeDonut(20f, 25f)];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            var center = Center;
            var distance = (caster.Position - center).Length();
            var activation = Module.CastFinishAt(spell);
            var pos = center.Quantized();
            if (distance is < 3 or > 9 and < 11 or > 17 and < 19) // tiles
            {
                for (var i = 0; i < 3; ++i)
                {
                    AddAOE(i);
                }
            }
            else
            {
                for (var i = 3; i < 6; ++i)
                {
                    AddAOE(i);
                }
            }
            void AddAOE(int index)
            {
                var shape = _shapes[index];
                AOEs.Add(new(shape, pos, default, activation, shapeDistance: shape.Distance(pos, default)));
            }
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            AOEs.Clear();
        }
    }
}
