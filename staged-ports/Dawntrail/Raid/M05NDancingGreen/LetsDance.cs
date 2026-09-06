// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M05NDancingGreen;

sealed class LetsDance(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeRect rect = new(25f, 45f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
        {
            return [];
        }
        var max = count > 2 ? 2 : count;
        return CollectionsMarshal.AsSpan(_aoes)[..max];
    }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor.OID == (uint)OID.Frogtourage && modelState is 5 or 7)
        {
            var count = _aoes.Count;
            var act = count != 0 ? _aoes.Ref(0).Activation.AddSeconds(count * 2d) : World.FutureTime(18.2d);
            var pos = Center.Quantized();
            var rot = modelState == 5 ? Angle.AnglesCardinals[3] : Angle.AnglesCardinals[0];
            _aoes.Add(new(rect, pos, rot, act, shapeDistance: count == 0 ? rect.Distance(pos, rot) : null));
            if (count == 1)
            {
                ref var aoe2 = ref _aoes.Ref(1);
                aoe2.Origin += 5f * rot.ToDirection();
                aoe2.ShapeDistance = rect.Distance(aoe2.Origin, rot);
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var count = _aoes.Count;
        if (count != 0 && spell.Action.ID == (uint)AID.LetsDance)
        {
            _aoes.RemoveAt(0);

            if (count > 1)
            {
                var aoes = CollectionsMarshal.AsSpan(_aoes);
                ref var aoe1 = ref aoes[0];
                var rot1 = aoe1.Rotation;
                aoe1.Origin -= 5f * rot1.ToDirection();
                aoe1.ShapeDistance = rect.Distance(aoe1.Origin, rot1);
                if (count > 2)
                {
                    ref var aoe2 = ref aoes[1];
                    var rot2 = aoe2.Rotation;
                    aoe2.Origin += 5f * rot2.ToDirection();
                    aoe2.ShapeDistance = rect.Distance(aoe2.Origin, rot2);
                }
            }
        }
    }
}
