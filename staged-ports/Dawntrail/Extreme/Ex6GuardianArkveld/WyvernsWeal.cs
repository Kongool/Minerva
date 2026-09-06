// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex6GuardianArkveld;

sealed class WyvernsWeal(ModuleBase module) : Components.GenericBaitAway(module)
{
    private readonly AOEShapeRect rect = new(60f, 3f);
    public int NumFinishedLasers;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.WyvernsWeal)
        {
            CurrentBaits.Add(new(Module.PrimaryActor, actor, rect, World.FutureTime(8.1d)));
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (CurrentBaits.Count != 0 && spell.Action.ID == (uint)AID.WyvernsWealFirst)
        {
            CurrentBaits.RemoveAt(0);
        }
    }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (actor == Module.PrimaryActor && modelState == 0 && animState1 == 0 && animState2 == 1)
        {
            ++NumFinishedLasers;
        }
    }
}

// I don't think its possible to predict the exact locations of this, so this might be the best we can do.
// the laser wil follow a random different player if the target dies
sealed class WyvernsWealAOE(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.WyvernsWealFirst, (uint)AID.WyvernsWealRepeat], new AOEShapeRect(60f, 3f));
