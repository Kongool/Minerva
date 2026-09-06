// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex2Garuda;

class EyeOfTheStorm(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.EyeOfTheStorm)
{
    private Actor? _caster;
    private DateTime _nextCastAt;
    private static readonly AOEShapeDonut _shape = new(12f, 25f); // TODO: verify inner radius

    public bool Active() => _caster?.CastInfo != null || _nextCastAt > World.CurrentTime;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_caster != null)
            return new AOEInstance[1] { new(_shape, _caster.Position.Quantized(), default, _nextCastAt) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _caster = caster;
            _nextCastAt = Module.CastFinishAt(caster.CastInfo!);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _nextCastAt = World.FutureTime(4.2d);
        }
    }
}
