// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A13ArkAngels;

sealed class DominionSlash(ModuleBase module) : Components.GenericAOEs(module)
{
    public readonly List<AOEInstance> AOEs = [];

    private static readonly AOEShapeCircle _shape = new(6f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00010002u && actor.OID == (uint)OID.DominionSlashHelper)
        {
            AOEs.Add(new(_shape, actor.Position, default, World.FutureTime(6.5d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.DivineDominion or (uint)AID.DivineDominionFail)
        {
            ++NumCasts;
            AOEs.RemoveAll(aoe => aoe.Origin == caster.Position);
        }
    }
}
