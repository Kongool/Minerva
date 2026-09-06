// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex3Titan;

// TODO: most of what's here should be handled by SimpleKnockbacks component...
class Upheaval(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.Upheaval)
{
    private DateTime _remainInPosition;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        if (_remainInPosition > World.CurrentTime)
            return new Knockback[1] { new(Module.PrimaryActor.Position, 13f) };
        return [];
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_remainInPosition > World.CurrentTime)
        {
            // stack just behind boss, this is a good place to bait imminent landslide correctly
            var dirToCenter = (Center - Module.PrimaryActor.Position).Normalized();
            var pos = Module.PrimaryActor.Position + 2f * dirToCenter;
            hints.AddForbiddenZone(new SDInvertedCircle(pos, 1.5f), _remainInPosition);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _remainInPosition = Module.CastFinishAt(spell, 1); // TODO: just wait for effectresult instead...
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _remainInPosition = World.FutureTime(1d); // TODO: just wait for effectresult instead...
    }
}
