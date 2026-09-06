// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C013Shadowcaster;

abstract class BlazingBenifice(ModuleBase module, uint aid, uint oid) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(100f, 5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }

        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var startTime = World.CurrentTime.AddSeconds(-5d);
        var deadline = aoes[0].Activation.AddSeconds(1d);
        var index = 0;
        while (index < count)
        {
            ref var aoe = ref aoes[index];
            var act = aoe.Activation;
            if (act < startTime || act >= deadline)
            {
                break;
            }
            ++index;
        }

        return aoes[..index];
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == oid)
        {
            _aoes.Add(new(rect, actor.Position.Quantized(), actor.Rotation, World.FutureTime(21.7d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == aid)
        {
            if (_aoes.Count != 0)
            {
                _aoes.RemoveAt(0);
            }
            ++NumCasts;
        }
    }
}

sealed class NBlazingBenifice(ModuleBase module) : BlazingBenifice(module, (uint)AID.NBlazingBenifice, (uint)OID.NArcaneFont);
sealed class SBlazingBenifice(ModuleBase module) : BlazingBenifice(module, (uint)AID.SBlazingBenifice, (uint)OID.SArcaneFont);
