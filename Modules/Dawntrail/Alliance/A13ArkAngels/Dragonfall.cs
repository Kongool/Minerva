// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A13ArkAngels;

sealed class Dragonfall(ModuleBase module) : Components.UniformStackSpread(module, 6f, default, 8, 8)
{

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.Dragonfall)
        {
            AddStack(source, World.FutureTime(9.5d));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.DragonfallAOE)
        {
            ++NumCasts;
            Stacks.RemoveAll(s => s.Target.InstanceID == spell.MainTargetID);
            var forbidden = Raid.WithSlot(false, false, true).WhereActor(a => spell.Targets.Any(t => t.ID == a.InstanceID)).Mask();
            foreach (ref var s in Stacks.AsSpan())
                s.ForbiddenPlayers |= forbidden;
        }
    }
}
