// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M03SBruteBomber;

sealed class KnuckleSandwich(ModuleBase module) : Components.GenericSharedTankbuster(module, default, 6f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.KnuckleSandwich)
        {
            Source = caster;
            Target = World.Actors.Find(spell.TargetID);
            Activation = Module.CastFinishAt(spell);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.KnuckleSandwich or (uint)AID.KnuckleSandwichAOE)
        {
            ++NumCasts;
        }
    }
}
