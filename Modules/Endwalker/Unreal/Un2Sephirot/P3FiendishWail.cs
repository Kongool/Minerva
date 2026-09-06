// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un2Sephirot;

class P3FiendishWail(ModuleBase module) : Components.CastCounter(module, (uint)AID.FiendishWailAOE)
{
    private BitMask _physResistMask;
    private readonly List<Actor> _towers = [];

    public bool Active => _towers.Count > 0;

    private const float _radius = 5;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!Active)
            return;

        var wantToSoak = _physResistMask.Any() ? _physResistMask[slot] : actor.Role == Role.Tank;
        var soaking = _towers.InRadius(actor.Position, _radius).Any();
        if (wantToSoak)
            hints.Add("Soak the tower!", !soaking);
        else
            hints.Add("GTFO from tower!", soaking);
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var t in _towers)
            Arena.ZoneCircleOutline(t.Position, _radius, Colors.Danger);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.ForceAgainstMight)
            _physResistMask.Set(Raid.FindSlot(actor.InstanceID));
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _towers.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _towers.Remove(caster);
    }
}
