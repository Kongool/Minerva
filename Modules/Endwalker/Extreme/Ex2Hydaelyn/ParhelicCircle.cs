// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex2Hydaelyn;

class ParhelicCircle(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCircle _circle = new(6);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        var activation = World.FutureTime(7.6d);
        var c = Center;
        if (actor.OID == (uint)OID.RefulgenceHexagon)
        {
            var dir = c + 17f * actor.Rotation.ToDirection();
            _aoes.Add(new(_circle, c.Quantized(), default, activation));
            for (var i = 1; i < 7; ++i)
                _aoes.Add(new(_circle, WPos.RotateAroundOrigin(i * 60f, c, dir.Quantized()), default, activation));
        }
        else if (actor.OID == (uint)OID.RefulgenceTriangle)
        {
            var dir = c + 8f * actor.Rotation.ToDirection();
            for (var i = 1; i < 4; ++i)
                _aoes.Add(new(_circle, WPos.RotateAroundOrigin(-60f + i * 120f, c, dir.Quantized()), default, activation));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Incandescence)
        {
            ++NumCasts;
            _aoes.Clear();
        }
    }
}
