// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex2ZoraalJa;

sealed class AeroIII(ModuleBase module) : Components.GenericKnockback(module)
{
    public readonly List<Actor> Voidzones = module.Enemies((uint)OID.BitingWind);

    private static readonly AOEShapeCircle _shape = new(4);

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        var count = Voidzones.Count;
        if (count == 0)
        {
            return [];
        }
        var sources = new Knockback[count];
        for (var i = 0; i < count; ++i)
        {
            sources[i] = new(Voidzones[i].Position, 25f, shape: _shape, ignoreImmunes: true);
        }
        return sources;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var count = Voidzones.Count;
        if (count == 0)
        {
            return;
        }
        for (var i = 0; i < count; ++i)
        {
            _shape.Outline(Arena, Voidzones[i].Position);
        }
        base.DrawArenaForeground(pcSlot, pc);
    }
}
