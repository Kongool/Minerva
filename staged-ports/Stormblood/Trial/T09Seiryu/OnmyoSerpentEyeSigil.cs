// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T09Seiryu;

sealed class OnmyoSerpentEyeSigil(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeDonut donut = new(7f, 30f);
    private static readonly AOEShapeCircle circle = new(12f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        AOEShape? shape = modelState switch
        {
            32 => circle,
            33 => donut,
            _ => null
        };
        if (shape != null)
        {
            _aoe = [new(shape, actor.Position.Quantized(), default, World.FutureTime(5.6d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.OnmyoSigil or (uint)AID.SerpentEyeSigil)
        {
            _aoe = [];
        }
    }
}
