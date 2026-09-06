// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Trial.T09WhorleaterH;

sealed class SpinningDive(ModuleBase module) : Components.GenericAOEs(module) // TODO: Find out how to detect spinning dives earlier eg. the water column telegraph
{
    private AOEInstance[] _aoe = [];
    public static readonly AOEShapeRect Rect = new(50.5f, 8f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.SpinningDiveHelper)
        {
            _aoe = [new(Rect, actor.Position.Quantized(), actor.Rotation, World.FutureTime(0.6d))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SpinningDiveSnapshot)
        {
            _aoe = [];
        }
    }
}

sealed class SpinningDiveKB(ModuleBase module) : Components.GenericKnockback(module, stopAtWall: true) // TODO: Find out how to detect spinning dives earlier eg. the water column telegraph
{
    private Knockback[] _kb = [];
    private readonly Hydroshot _aoe1 = module.FindComponent<Hydroshot>()!;
    private readonly Dreadstorm _aoe2 = module.FindComponent<Dreadstorm>()!;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kb;

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.SpinningDiveHelper)
        {
            _kb = [new(actor.Position.Quantized(), 10f, World.FutureTime(1.4d), SpinningDive.Rect, actor.Rotation)];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SpinningDiveEffect)
        {
            _kb = [];
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes1 = _aoe1.ActiveAOEs(slot, actor);
        var len1 = aoes1.Length;
        for (var i = 0; i < len1; ++i)
        {
            ref readonly var aoe = ref aoes1[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        var aoes2 = _aoe2.ActiveAOEs(slot, actor);
        var len2 = aoes2.Length;
        for (var i = 0; i < len2; ++i)
        {
            ref readonly var aoe = ref aoes2[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return false;
    }
}
