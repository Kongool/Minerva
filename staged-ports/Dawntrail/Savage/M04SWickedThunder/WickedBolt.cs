// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M04SWickedThunder;

sealed class WickedBolt(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.WickedBolt, (uint)AID.WickedBoltAOE, 5, 5.1f, 8, 8)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == StackAction)
            ++NumFinishedStacks;
    }
}

sealed class WickedBlaze(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.WickedBolt, (uint)AID.WickedBlazeAOE, 10, 5.1f, 4, 4)
{
    private DateTime _nextNonDuplicate; // we only want to count individual ticks, so if only 1 person is alive, it doesn't fuck up the states

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == StackAction && World.CurrentTime > _nextNonDuplicate)
        {
            ++NumFinishedStacks;
            _nextNonDuplicate = World.FutureTime(0.5d);
        }
    }
}
