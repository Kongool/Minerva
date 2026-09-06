// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A34Eulogia;

sealed class Hydrostasis(ModuleBase module) : Components.GenericKnockback(module)
{
    private readonly List<Knockback> _sources = [];

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => CollectionsMarshal.AsSpan(_sources);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.HydrostasisAOE1 or (uint)AID.HydrostasisAOE2 or (uint)AID.HydrostasisAOE3)
        {
            _sources.Add(new(caster.Position, 28f, Module.CastFinishAt(spell)));
            _sources.Sort(static (a, b) => a.Activation.CompareTo(b.Activation));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.HydrostasisAOE1 or (uint)AID.HydrostasisAOE2 or (uint)AID.HydrostasisAOE3)
        {
            ++NumCasts;
            if (_sources.Count != 0)
            {
                _sources.RemoveAt(0);
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = _sources.Count;
        if (count != 0)
        {
            var casters = CollectionsMarshal.AsSpan(_sources);
            for (var i = 0; i < count; ++i)
            {
                ref readonly var c = ref casters[i];
                var act = c.Activation;
                var center = Center;
                if (!IsImmune(slot, act))
                {
                    if (count > i + 1)
                    {
                        ref readonly var c2 = ref casters[i + 1];
                        hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOriginIntoDirection(center, c.Origin, 28f, 29f, (c2.Origin - c.Origin).Normalized(), 15f.Degrees()), act);
                    }
                    else
                    {
                        hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(center, c.Origin, 28f, 29f), act);
                    }
                }
            }
        }
    }
}
