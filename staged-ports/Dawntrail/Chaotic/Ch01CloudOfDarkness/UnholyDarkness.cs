// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class UnholyDarkness(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.UnholyDarkness, (uint)AID.UnholyDarknessAOE, 6f, 8.1d, 4)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == StackAction)
        {
            Stacks.Clear(); // if one of the target dies, it won't get hit
            ++NumFinishedStacks;
        }
    }
}
