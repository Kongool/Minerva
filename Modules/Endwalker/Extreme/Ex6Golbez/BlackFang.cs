// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex6Golbez;

class BlackFang(ModuleBase module) : Components.CastCounter(module, default)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.BlackFangAOE1 or AID.BlackFangAOE2 or AID.BlackFangAOE3 or AID.BlackFangEnrageAOE1 or AID.BlackFangEnrageAOE2 or AID.BlackFangEnrageAOE3)
            ++NumCasts;
    }
}
