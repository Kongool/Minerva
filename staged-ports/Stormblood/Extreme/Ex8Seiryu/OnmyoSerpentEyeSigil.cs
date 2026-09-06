// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex8Seiryu;

sealed class OnmyoSerpentEyeSigil(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeDonut donut = new(7f, 30f);
    private static readonly AOEShapeCircle circle = new(12f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }
        return CollectionsMarshal.AsSpan(_aoes)[..1];
    }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        void AddAOE(AOEShape shape, bool first = true) => _aoes.Add(new(shape, actor.Position.Quantized(), default, World.FutureTime(first ? 5.6d : 8.7d)));
        switch (modelState)
        {
            case 5:
                AddAOE(donut);
                AddAOE(circle, false);
                break;
            case 6:
                AddAOE(circle);
                AddAOE(donut, false);
                break;
            case 32:
                AddAOE(circle);
                break;

        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.OnmyoSigil or (uint)AID.OnmyoSigilFirst or (uint)AID.OnmyoSigilSecond or (uint)AID.SerpentEyeSigilFirst or (uint)AID.SerpentEyeSigilSecond)
        {
            ++NumCasts;
            if (_aoes.Count > 1)
            {
                _aoes.RemoveAt(0);
            }
        }
    }
}
