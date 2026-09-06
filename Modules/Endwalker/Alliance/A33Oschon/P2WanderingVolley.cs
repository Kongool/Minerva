// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A33Oschon;

sealed class P2WanderingVolleyDownhill(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WanderingVolleyDownhillAOE, 8f);

sealed class P2WanderingVolleyKnockback(ModuleBase module) : Components.GenericKnockback(module)
{
    private readonly P2WanderingVolleyDownhill? _downhill = module.FindComponent<P2WanderingVolleyDownhill>();
    private readonly List<Knockback> _sources = [];
    private static readonly AOEShapeCone _shape = new(30f, 90f.Degrees());

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => CollectionsMarshal.AsSpan(_sources);

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        if (_downhill == null)
            return false;
        var aoes = _downhill.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WanderingVolleyN or (uint)AID.WanderingVolleyS)
        {
            _sources.Clear();
            // happens through center, so create two sources with origin at center looking orthogonally
            _sources.Add(new(Center, 12f, Module.CastFinishAt(spell), _shape, spell.Rotation + 90f.Degrees(), Kind.DirForward));
            _sources.Add(new(Center, 12f, Module.CastFinishAt(spell), _shape, spell.Rotation - 90f.Degrees(), Kind.DirForward));
        }
    }
}

sealed class P2WanderingVolleyAOE(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeRect _shape = new(40f, 5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WanderingVolleyN or (uint)AID.WanderingVolleyS)
        {
            _aoe = [new(_shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WanderingVolleyN or (uint)AID.WanderingVolleyS)
        {
            _aoe = [];
            ++NumCasts;
        }
    }
}
