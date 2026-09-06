// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A35FalseIdol;

sealed class UnevenFooting(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeRect rect = new(80f, 15f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.UnevenFooting)
        {
            var rot = actor.Rotation - 90f.Degrees();
            _aoe = [new(rect, (actor.Position - 40f * rot.ToDirection()).Quantized(), rot, World.FutureTime(13.2d), risky: false)];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.UnevenFooting)
        {
            _aoe = [];
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00040008u && actor.OID == (uint)OID.UnevenFooting && _aoe.Length != 0)
        {
            ref var aoe = ref _aoe[0];
            aoe.Risky = true;
        }
    }
}
