// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UCOB;

class P5AhkMorn(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.AkhMorn, 4f)
{
    // cast is only a first hit, don't deactivate
    public override void OnCastFinished(Actor caster, ActorCastInfo spell) { }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.AkhMorn or (uint)AID.AkhMornAOE)
            ++NumCasts;
    }
}
