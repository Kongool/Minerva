// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS2StygimolochWarrior;

sealed class UnrelentingCharge(ModuleBase module) : Components.GenericKnockback(module)
{
    private Actor? _source;
    private DateTime _activation;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_source != null)
            return new Knockback[1] { new(_source.Position, 10, _activation) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.UnrelentingCharge)
        {
            _source = caster;
            _activation = Module.CastFinishAt(spell, 0.3f);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.UnrelentingChargeAOE)
        {
            ++NumCasts;
            _activation = World.FutureTime(1.6d);
        }
    }
}
