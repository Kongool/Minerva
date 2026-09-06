// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Trial.T02Hydaelyn;

class Echoes(ModuleBase module) : Components.UniformStackSpread(module, 6f, 0, 8, 8)
{

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Echoes)
        {
            ++NumCasts;
            if (NumCasts == 5)
            {
                Stacks.Clear();
                NumCasts = 0;
            }
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Echoes)
            AddStack(actor, World.FutureTime(5));
    }
}
