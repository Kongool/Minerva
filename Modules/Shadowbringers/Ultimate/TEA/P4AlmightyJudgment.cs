// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

// note: sets are 2s apart, 8-9 casts per set
[SkipLocalsInit]
sealed class P4AlmightyJudgment(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private readonly AOEShapeCircle circle = new(6f);

    public bool Active => _aoes.Count > 0;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var deadline = aoes[0].Activation.AddSeconds(3d);

        var index = 0;
        while (index < count)
        {
            ref var aoe = ref aoes[index];
            if (aoe.Activation >= deadline)
            {
                break;
            }
            ++index;
        }

        return aoes[..index];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.AlmightyJudgmentVisual)
        {
            _aoes.Add(new(circle, spell.LocXZ, default, World.FutureTime(8d)));
            var count = _aoes.Count;
            if (count is 8 or 9) // there can be 8 or 9 aoes in a wave
            {
                var aoes = CollectionsMarshal.AsSpan(_aoes);
                var deadline = aoes[0].Activation.AddSeconds(1d);
                var color = Colors.Danger;
                for (var i = 0; i < count; ++i)
                {
                    ref var aoe = ref aoes[i];
                    if (aoe.Activation < deadline)
                    {
                        aoe.Color = color;
                    }
                }
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.AlmightyJudgmentAOE)
        {
            var count = _aoes.Count;
            var aoes = CollectionsMarshal.AsSpan(_aoes);
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                if (pos.AlmostEqual(aoes[i].Origin, 1f))
                {
                    _aoes.RemoveAt(i);
                    break;
                }
            }
            count = _aoes.Count;
            if (count is 16 or 17)
            {
                aoes = CollectionsMarshal.AsSpan(_aoes);
                ref var aoe0 = ref aoes[0];
                var deadline = aoe0.Activation.AddSeconds(1d);
                if (aoes[1].Activation > deadline)
                {
                    var index = 0;
                    var lastAct = aoes[^1].Activation.AddSeconds(-1d);
                    var color = Colors.Danger;
                    while (index < count)
                    {
                        ref var aoe = ref aoes[index];
                        if (aoe.Activation <= lastAct)
                        {
                            aoe.Color = color;
                        }
                        ++index;
                    }
                }
            }
        }
    }
}
