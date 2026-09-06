// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P3Tetrashatter(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private readonly AOEShapeCircle circle = new(21f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.JudgmentCrystal)
        {
            _aoes.Add(new(circle, actor.Position.Quantized(), default, World.FutureTime(5.3d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Tetrashatter)
        {
            var count = _aoes.Count;
            var aoes = CollectionsMarshal.AsSpan(_aoes);
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                if (pos.AlmostEqual(aoes[i].Origin, 1f))
                {
                    _aoes.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
