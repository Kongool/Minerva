// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS8Queen;

// limiting to one aoe since there are 2 casters to circumvent aoe target limit
sealed class HeavensWrathAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeavensWrathKnockback, new AOEShapeRect(60f, 5f), 1);

// TODO: generalize
sealed class HeavensWrathKnockback(ModuleBase module) : Components.GenericKnockback(module)
{
    private readonly List<Knockback> _sources = [];
    private static readonly AOEShapeRect _shape = new(60f, 50f, -5f);

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => CollectionsMarshal.AsSpan(_sources);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (_sources.Count == 0 && spell.Action.ID == (uint)AID.HeavensWrathKnockback)
        {
            var act = Module.CastFinishAt(spell);
            _sources.Add(new(caster.Position, 15f, act, _shape, spell.Rotation + 90f.Degrees(), Kind.DirForward));
            _sources.Add(new(caster.Position, 15f, act, _shape, spell.Rotation - 90f.Degrees(), Kind.DirForward));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HeavensWrathKnockback)
        {
            _sources.Clear();
            ++NumCasts;
        }
    }
}
