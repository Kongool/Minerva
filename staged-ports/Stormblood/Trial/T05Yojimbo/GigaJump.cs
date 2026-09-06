// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class GigaJump(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Icon87, 0, 25f, 6f)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.GigaJump1 or (uint)AID.GigaJump2)
            Spreads.Clear();
    }
}
