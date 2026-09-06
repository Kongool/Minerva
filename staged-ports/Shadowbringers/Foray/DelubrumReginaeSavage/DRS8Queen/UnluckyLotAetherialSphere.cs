// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS8Queen;

sealed class UnluckyLotAetherialSphere(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.UnluckyLotAetherialSphere)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeCircle circle = new(20f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.OptimalOffensiveMoveSphere)
        {
            _aoe = [new(circle, spell.LocXZ, default, Module.CastFinishAt(spell, 2.6d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.UnluckyLotAetherialSphere)
        {
            _aoe = [];
        }
    }
}
