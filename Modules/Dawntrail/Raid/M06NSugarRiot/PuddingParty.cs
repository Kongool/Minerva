// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M06NSugarRiot;

sealed class PuddingParty(ModuleBase module) : Components.UniformStackSpread(module, 6f, default, 8, 8)
{
    private int numCasts;
    private bool first = true;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.PuddingParty)
            AddStack(actor, World.FutureTime(5.1d));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.PuddingParty)
        {
            ++numCasts;
            if (first && numCasts == 5 || numCasts == 6)
            {
                Stacks.Clear();
                numCasts = 0;
                first = false;
            }
        }
    }
}
