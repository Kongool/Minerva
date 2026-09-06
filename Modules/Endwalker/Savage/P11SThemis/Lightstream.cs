// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P11SThemis;

class Lightstream(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShapeRect _shape = new(50f, 5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.LightstreamAOEFirst or (uint)AID.LightstreamAOERest)
        {
            if (_aoes.Count > 0)
                _aoes.RemoveAt(0);
            ++NumCasts;
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        var rotation = iconID switch
        {
            (uint)IconID.RotateCW => -10f.Degrees(),
            (uint)IconID.RotateCCW => 10f.Degrees(),
            _ => default
        };
        if (rotation != default)
        {
            for (var i = 0; i < 7; ++i)
                _aoes.Add(new(_shape, actor.Position.Quantized(), actor.Rotation + i * rotation, World.FutureTime(8d + i * 1.1d)));
            SortHelpers.SortAOEByActivation(_aoes);
        }
    }
}
