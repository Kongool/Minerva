// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN6Queen;

sealed class MeansEnds(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCross cross = new(60f, 5f);
    private readonly Chess _chess = module.FindComponent<Chess>()!;
    private bool shortActivation;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var aoesSlot = _chess.AOEs[slot];

        if (aoesSlot != default && aoesSlot.Count != 0)
        {
            var count = _aoes.Count;
            for (var i = 0; i < count; ++i)
            {
                aoes[i].Risky = true;
            }
        }
        return aoes;
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (_aoes.Count != 2 && status.ID == (uint)SID.MovementIndicator)
        {
            var distance = status.Extra switch
            {
                0xE2 => 1,
                0xE3 => 2,
                0xE4 => 3,
                _ => 0
            };
            if (distance != 0)
            {
                var rot = actor.Rotation;
                _aoes.Add(new(cross, (actor.Position + distance * 10f * rot.ToDirection()).Quantized(), rot, World.FutureTime(shortActivation ? 11.7d : 22d), risky: shortActivation));
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.QueensWill)
            shortActivation = true;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.EndsKnight or (uint)AID.EndsSoldier or (uint)AID.MeansGunner or (uint)AID.MeansWarrior)
        {
            _aoes.Clear();
            shortActivation = false;
        }
    }
}
