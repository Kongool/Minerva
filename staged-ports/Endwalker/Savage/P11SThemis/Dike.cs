// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P11SThemis;

class Dike(ModuleBase module) : Components.CastCounter(module, (uint)AID.DikeSecond)
{
    private ulong _firstPrimaryTarget;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Module.PrimaryActor.TargetID == _firstPrimaryTarget && actor.Role == Role.Tank)
            hints.Add(actor.InstanceID != _firstPrimaryTarget ? "Taunt!" : "Pass aggro!");
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.DikeAOE1Primary)
            _firstPrimaryTarget = spell.TargetID;
    }
}
