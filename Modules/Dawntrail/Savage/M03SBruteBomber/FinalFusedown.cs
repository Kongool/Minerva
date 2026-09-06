// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M03SBruteBomber;

sealed class FinalFusedownSelfDestruct(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShapeCircle _shape = new(8);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];
        var max = count > 4 ? 4 : count;
        return CollectionsMarshal.AsSpan(_aoes)[..max];
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var delay = status.ID switch
        {
            (uint)SID.FinalFusedownFutureSelfDestructShort => 12.2d,
            (uint)SID.FinalFusedownFutureSelfDestructLong => 17.2d,
            _ => default
        };
        if (delay > 0)
        {
            _aoes.Add(new(_shape, actor.Position.Quantized(), default, World.FutureTime(delay)));
            SortHelpers.SortAOEByActivation(_aoes);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.FinalFusedownSelfDestructShort or (uint)AID.FinalFusedownSelfDestructLong)
        {
            ++NumCasts;
            if (_aoes.Count != 0)
                _aoes.RemoveAt(0);
        }
    }
}

sealed class FinalFusedownExplosion(ModuleBase module) : Components.GenericStackSpread(module, true)
{
    private readonly List<Spread> _spreads1 = [];
    private readonly List<Spread> _spreads2 = [];

    public void Show() => Spreads = _spreads1;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        (var list, var delay) = status.ID switch
        {
            (uint)SID.FinalFusedownFutureExplosionShort => (_spreads1, 12.2d),
            (uint)SID.FinalFusedownFutureExplosionLong => (_spreads2, 17.2d),
            _ => (null, default)
        };
        list?.Add(new(actor, 6f, World.FutureTime(delay)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.FinalFusedownExplosionShort:
                ++NumCasts;
                Spreads = _spreads2;
                break;
            case (uint)AID.FinalFusedownExplosionLong:
                ++NumCasts;
                Spreads.Clear();
                break;
        }
    }
}
