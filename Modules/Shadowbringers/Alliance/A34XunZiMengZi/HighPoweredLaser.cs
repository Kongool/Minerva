// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A34XunZiMengZi;

sealed class HighPoweredLaser(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> casters = [];
    private static readonly AOEShapeRect rect = new(70f, 2f);
    private DateTime activation;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = casters.Count;
        if (count == 0)
        {
            return [];
        }
        var aoes = new AOEInstance[count];
        var isRisky = World.CurrentTime > activation.AddSeconds(-1d); // lasers stop tracking about 1s before activation
        for (var i = 0; i < count; ++i)
        {
            var c = casters[i];
            aoes[i] = new(rect, c.Position.Quantized(), c.Rotation, activation, risky: isRisky);
        }
        return aoes;
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.Tracking)
        {
            casters.Add(actor);
            activation = World.FutureTime(6.6d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.HighPoweredLaser)
        {
            casters.Clear();
            activation = default;
        }
    }
}
