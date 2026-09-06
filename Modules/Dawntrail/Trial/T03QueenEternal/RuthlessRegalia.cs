// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T03QueenEternal;

sealed class RuthlessRegalia(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect rect = new(100f, 6f), rectWide = new(100f, 12f);
    private (Actor, DateTime)? _source;
    private readonly List<Actor> _tethered = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_source != null)
        {
            var s = _source.Value;
            return new AOEInstance[1] { new(rect, s.Item1.Position, s.Item1.Rotation, s.Item2) };
        }
        return [];
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.QueenEternal2 && id == 0x11D2)
            _source = new(actor, World.FutureTime(11.1d));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.RuthlessRegalia)
            _source = null;
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        _tethered.Add(source);
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        _tethered.Clear();
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_source == null)
            return;

        if (_tethered.Count > 1 && _tethered.Contains(actor))
        {
            var s = _source.Value.Item1;
            var t = _tethered.FirstOrDefault(x => x != actor);
            ref var sPosRot = ref s.PosRot;
            hints.AddForbiddenZone(rectWide, new(sPosRot.X, t!.PosRot.Z), sPosRot.W.Degrees());
        }
        else
        {
            base.AddAIHints(slot, actor, assignment, hints);
        }
    }
}
