// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex2ZoraalJa;

sealed class ProjectionOfTriumph(ModuleBase module) : Components.GenericAOEs(module)
{
    private record struct Line(WDir Direction, AOEShape Shape);

    private readonly List<Line> _lines = [];
    private DateTime _nextActivation;

    private static readonly AOEShapeCircle _shapeCircle = new(4f);
    private static readonly AOEShapeDonut _shapeDonut = new(3f, 8f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var nextOrder = NextOrder();
        var count = _lines.Count;
        var aoes = new List<AOEInstance>();
        for (var i = 0; i < count; ++i)
        {
            var order = i >= 2 ? nextOrder - 2 : nextOrder;
            if (order is >= 0 and < 4)
            {
                var line = _lines[i];
                var lineCenter = Center + (-15f + 10f * order) * line.Direction;
                var ortho = line.Direction.OrthoL();
                for (var j = -15; j <= 15; j += 10)
                {
                    aoes.Add(new(line.Shape, (lineCenter + j * ortho).Quantized(), default, _nextActivation));
                }
            }
        }
        return CollectionsMarshal.AsSpan(aoes);
    }

    public override void OnActorCreated(Actor actor)
    {
        AOEShape? shape = actor.OID switch
        {
            (uint)OID.ProjectionOfTriumphCircle => _shapeCircle,
            (uint)OID.ProjectionOfTriumphDonut => _shapeDonut,
            _ => null
        };
        if (shape != null)
        {
            _lines.Add(new(actor.Rotation.ToDirection(), shape));
            _nextActivation = World.FutureTime(9d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.SiegeOfVollok or (uint)AID.WallsOfVollok)
        {
            ++NumCasts;
            _nextActivation = World.FutureTime(5d);
        }
    }

    public int NextOrder() => NumCasts switch
    {
        < 8 => 0,
        < 16 => 1,
        < 32 => 2,
        < 48 => 3,
        < 56 => 4,
        < 64 => 5,
        _ => 6
    };
}
