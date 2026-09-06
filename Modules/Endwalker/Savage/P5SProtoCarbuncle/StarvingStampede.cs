// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P5SProtoCarbuncle;

class StarvingStampede(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.StarvingStampede)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShape _shape = new AOEShapeCircle(12f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];
        var max = count > 3 ? 3 : count;
        return CollectionsMarshal.AsSpan(_aoes)[..max];
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == (uint)AID.JawsTeleport)
        {
            if (_aoes.Count == 0)
                _aoes.Add(new(_shape, caster.Position));
            _aoes.Add(new(_shape, spell.TargetXZ));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == WatchedAction)
            _aoes.RemoveAt(0);
    }
}
