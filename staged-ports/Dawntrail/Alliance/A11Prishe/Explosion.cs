// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A11Prishe;

sealed class Explosion(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.Explosion)
{
    private static readonly AOEShapeCircle circle = new(8f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];
        var act0 = _aoes[0].Activation;
        var compareFL = (_aoes[count - 1].Activation - act0).TotalSeconds > 1d;
        var aoes = new AOEInstance[count];
        var color = Colors.Danger;
        for (var i = 0; i < count; ++i)
        {
            var aoe = _aoes[i];
            aoes[i] = (aoe.Activation - act0).TotalSeconds < 1d ? aoe with { Color = compareFL ? color : 0 } : aoe with { Risky = false };
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Explosion)
            _aoes.Add(new(circle, spell.LocXZ, default, Module.CastFinishAt(spell)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.Explosion)
            _aoes.RemoveAt(0);
    }
}
