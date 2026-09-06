// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un4Zurvan;

class P2DemonsClawKnockback(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.DemonsClaw)
{
    private Actor? _caster;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_caster?.CastInfo?.TargetID == actor.InstanceID)
            return new Knockback[1] { new(_caster.Position, 17f, Module.CastFinishAt(_caster.CastInfo), ignoreImmunes: true) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _caster = caster;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _caster = null;
    }
}

class P2DemonsClawWaveCannon(ModuleBase module) : Components.GenericWildCharge(module, 5f, (uint)AID.WaveCannonShared)
{
    public Actor? Target;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            Source = caster;
            foreach (var (slot, player) in Raid.WithSlot(false, true, true))
            {
                PlayerRoles[slot] = player == Target ? PlayerRole.Target : PlayerRole.Share;
            }
        }
        else if ((AID)spell.Action.ID == AID.DemonsClaw)
        {
            Target = World.Actors.Find(spell.TargetID);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            Source = null;
    }
}
