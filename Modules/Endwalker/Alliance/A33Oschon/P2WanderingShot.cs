// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A33Oschon;

sealed class P2WanderingShot(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.GreatWhirlwind)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeCircle _shape = new(23f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var coords = spell.Action.ID switch
        {
            (uint)AID.WanderingShotN or (uint)AID.WanderingVolleyN => new WPos(default, 740f).Quantized(),
            (uint)AID.WanderingShotS or (uint)AID.WanderingVolleyS => new WPos(default, 760f).Quantized(),
            _ => default
        };
        if (coords != default)
        {
            _aoe = [new(_shape, coords, default, Module.CastFinishAt(spell, 3.6d))];
        }
    }
}
