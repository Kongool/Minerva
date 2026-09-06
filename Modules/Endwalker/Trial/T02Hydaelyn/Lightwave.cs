// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Trial.T02Hydaelyn;

class Lightwave(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> waves = [];
    private static readonly AOEShapeRect rect = new(16f, 8f, 12f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = waves.Count;
        if (waves.Count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var w = waves[i];
            aoes[i] = new(rect, w.Position, w.Rotation, World.FutureTime(1.1d));
        }
        return aoes;
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.RayOfLight && !waves.Contains(caster))
            waves.Add(caster);
    }
}
