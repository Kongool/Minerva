// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN6Queen;

sealed class AboveBoard(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCircle circle = new(10f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ReversalOfForcesExtra)
        {
            _aoes.Clear();
            var bombs = Module.Enemies((uint)OID.AetherialBurst);
            var count = bombs.Count;
            var activation = Module.CastFinishAt(spell, 15.1d);
            for (var i = 0; i < count; ++i)
            {
                _aoes.Add(new(circle, bombs[i].Position.Quantized(), default, activation));
            }
        }
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.AetherialBolt)
        {
            _aoes.Add(new(circle, actor.Position.Quantized(), default, World.FutureTime(21d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.LotsCastBigLong or (uint)AID.LotsCastSmallLong)
        {
            _aoes.Clear();
        }
    }
}
