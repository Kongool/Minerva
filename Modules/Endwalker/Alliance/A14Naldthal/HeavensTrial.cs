// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A14Naldthal;

class HeavensTrialCone(ModuleBase module) : Components.GenericBaitAway(module)
{
    private static readonly AOEShapeCone _shape = new(60f, 15f.Degrees());

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.HeavensTrialConeStart:
                var target = World.Actors.Find(spell.MainTargetID);
                if (target != null)
                    CurrentBaits.Add(new(caster, target, _shape));
                break;
            case (uint)AID.HeavensTrialSmelting:
                CurrentBaits.Clear();
                ++NumCasts;
                break;
        }
    }
}

class HeavensTrialStack(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.HeavensTrialAOE, 6f, 8, 8);
