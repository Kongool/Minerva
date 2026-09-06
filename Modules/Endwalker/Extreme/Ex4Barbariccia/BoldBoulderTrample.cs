// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex4Barbariccia;

class BoldBoulderTrample(ModuleBase module) : Components.UniformStackSpread(module, 6f, 20f, 6, 6)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.BoldBoulder && World.Actors.Find(spell.TargetID) is var target && target != null)
            AddSpread(target);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.BoldBoulder)
            Spreads.RemoveAll(s => s.Target.InstanceID == spell.TargetID);
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Trample)
            AddStack(actor);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Trample)
            Stacks.RemoveAll(s => s.Target.InstanceID == spell.MainTargetID);
    }
}
