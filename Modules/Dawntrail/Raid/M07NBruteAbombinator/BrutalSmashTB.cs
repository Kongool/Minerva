// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M07NBruteAbombinator;

sealed class BrutalSmashTB(ModuleBase module) : Components.GenericSharedTankbuster(module, default, 6f)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.BrutalSmashTB)
        {
            Source = Module.PrimaryActor;
            Target = actor;
            Activation = World.FutureTime(5.9d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.BrutalSmashTB1 or (uint)AID.BrutalSmashTB2)
        {
            Source = null;
            Target = null;
        }
    }
}
