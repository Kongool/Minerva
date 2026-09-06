// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex1Ultima;

class CrimsonCyclone(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.CrimsonCyclone)
{
    private Actor? _ifrit; // non-null while mechanic is active
    private DateTime _resolve;

    public bool Active => _ifrit != null;

    private static readonly AOEShapeRect _shape = new(43f, 6f, 5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_ifrit != null)
            return new AOEInstance[1] { new(_shape, _ifrit.Position, _ifrit.Rotation, _resolve) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _ifrit = caster;
            _resolve = Module.CastFinishAt(spell);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _ifrit = null;
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.UltimaIfrit && id == 0x008D)
        {
            _ifrit = actor;
            _resolve = World.FutureTime(5d);
        }
    }
}
