// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P5SProtoCarbuncle;

class VenomousMass(ModuleBase module) : Components.CastCounter(module, (uint)AID.VenomousMassAOE)
{
    private Actor? _target;

    private const float _radius = 6;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_target != null && _target != actor && actor.Position.InCircle(_target.Position, _radius))
            hints.Add("GTFO from tankbuster!");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (_target != null)
            Arena.ZoneCircleOutline(_target.Position, _radius, Colors.Danger);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.VenomousMass)
            _target = World.Actors.Find(caster.TargetID);
    }
}
