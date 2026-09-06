// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A24Menphina;

class LunarKiss(ModuleBase module) : Components.GenericBaitAway(module)
{
    private static readonly AOEShapeRect _shape = new(60f, 3f);

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.LunarKiss)
            CurrentBaits.Add(new(Module.PrimaryActor, actor, _shape));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.LunarKissAOE)
        {
            ++NumCasts;
            CurrentBaits.Clear();
        }
    }
}
