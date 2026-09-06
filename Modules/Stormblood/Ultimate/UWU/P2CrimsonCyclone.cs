// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

// crimson cyclone has multiple variations:
// p2 first cast is a single charge along cardinal
// p2 second cast is two charges along both cardinals
// p2 third cast is four staggered charges, with different patterns depending on whether awakening happened (TODO: we can predict that very early)
// p4 predation is a single awakened charge along intercardinal
class CrimsonCyclone(ModuleBase module, float predictionDelay) : Components.GenericAOEs(module, (uint)AID.CrimsonCyclone)
{
    private readonly float _predictionDelay = predictionDelay;
    private readonly List<(AOEShape shape, WPos pos, Angle rot, DateTime activation)> _predicted = []; // note: there could be 1/2/4 predicted normal charges and 0 or 2 'cross' charges
    private readonly List<Actor> _casters = [];

    private static readonly AOEShapeRect _shapeMain = new(49, 9, 5);
    private static readonly AOEShapeRect _shapeCross = new(44.5f, 5, 0.5f);

    public bool CastsPredicted => _predicted.Count > 0;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var predictedCount = _predicted.Count <= 2 ? _predicted.Count : 0; // don't draw 4 predicted charges, it is pointless
        var casterCount = _casters.Count;
        var totalCount = predictedCount + casterCount;

        if (totalCount == 0)
            return [];

        var aoes = new AOEInstance[totalCount];
        var index = 0;

        for (var i = 0; i < predictedCount; ++i)
        {
            var p = _predicted[i];
            aoes[index++] = new(p.shape, p.pos, p.rot, p.activation);
        }

        for (var i = 0; i < casterCount; ++i)
        {
            var c = _casters[i];
            aoes[index++] = new(_shapeMain, c.Position, c.CastInfo!.Rotation, Module.CastFinishAt(c.CastInfo));
        }
        return aoes;
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (NumCasts == 0 && actor.OID == (uint)OID.Ifrit && id == 0x1E43)
            _predicted.Add((_shapeMain, actor.Position, actor.Rotation, World.FutureTime(_predictionDelay)));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            if (NumCasts == 0)
                _predicted.Clear();
            _casters.Add(caster);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _casters.Remove(caster);
            if (caster == ((UWU)Module).Ifrit() && caster.FindStatus((uint)SID.Woken) != null)
            {
                var act = World.FutureTime(2.2d);
                var a45 = spell.Rotation + 45f.Degrees();
                var am45 = spell.Rotation - 45f.Degrees();
                _predicted.Add((_shapeCross, Center - 19.5f * a45.ToDirection(), a45, act));
                _predicted.Add((_shapeCross, Center - 19.5f * am45.ToDirection(), am45, act));
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == (uint)AID.CrimsonCycloneCross)
        {
            _predicted.Clear();
        }
    }
}

class P2CrimsonCyclone(ModuleBase module) : CrimsonCyclone(module, 5.2f);
class P4CrimsonCyclone(ModuleBase module) : CrimsonCyclone(module, 8.1f);
