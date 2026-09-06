// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A22UltimaOmega;

sealed class SurfaceMissile(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeRect rect = new(12f, 10f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }
        var max = count > 4 ? 4 : count;
        return CollectionsMarshal.AsSpan(_aoes)[..max];
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.SurfaceMissile)
        {
            var delay = _aoes.Count switch
            {
                < 4 => 10.1d,
                < 8 => 9.2d,
                _ => 8.2d
            };
            var rot = actor.Rotation;
            _aoes.Add(new(rect, (actor.Position - 6f * rot.Round(1f).ToDirection()).Quantized(), rot, World.FutureTime(delay)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.SurfaceMissile)
        {
            _aoes.RemoveAt(0);
        }
    }
}
