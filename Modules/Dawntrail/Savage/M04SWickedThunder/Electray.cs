// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M04SWickedThunder;

sealed class Electray(ModuleBase module) : Components.GenericAOEs(module)
{
    public readonly List<AOEInstance> AOEs = [];

    private static readonly AOEShapeRect _shape = new(40f, 2.5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.GunBattery)
            AOEs.Add(new(_shape, actor.Position.Quantized(), actor.Rotation, World.FutureTime(6.8d), actorID: actor.InstanceID));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Electray)
        {
            ++NumCasts;
            var count = AOEs.Count;
            var id = caster.InstanceID;
            for (var i = 0; i < count; ++i)
            {
                if (AOEs[i].ActorID == id)
                {
                    AOEs.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
