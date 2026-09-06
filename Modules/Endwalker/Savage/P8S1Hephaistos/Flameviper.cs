// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S1Hephaistos;

class Flameviper(ModuleBase module) : Components.CastCounter(module, (uint)AID.FlameviperSecond)
{
    private ulong _firstTarget;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (actor.Role != Role.Tank)
            return;

        if (Module.PrimaryActor.TargetID == _firstTarget)
            hints.Add(actor.InstanceID == _firstTarget ? "Pass aggro to co-tank" : "Taunt!");
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.Flameviper)
            _firstTarget = spell.TargetID;
    }
}
