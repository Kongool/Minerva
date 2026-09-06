// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.TOP;

// TODO: not sure how exactly second target is selected, I think it is snapshotted to the current target when first cast happens?
// TODO: consider generalizing - same as P12S1 Glaukopis and others...
sealed class P5SolarRay(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private static readonly AOEShapeCircle _shape = new(5);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (NumCasts == 0 && CurrentBaits.FirstOrDefault(b => b.Source.TargetID == b.Target.InstanceID) is var b && b.Source != null && actor.Role == Role.Tank)
            hints.Add(b.Source.TargetID != actor.InstanceID ? "Taunt!" : "Pass aggro!");
        base.AddHints(slot, actor, hints);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.P5SolarRayM or (uint)AID.P5SolarRayF)
        {
            var target = World.Actors.Find(spell.TargetID);
            if (target != null)
                CurrentBaits.Add(new(caster, target, _shape));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.P5SolarRayM or (uint)AID.P5SolarRayMSecond or (uint)AID.P5SolarRayF or (uint)AID.P5SolarRayFSecond)
        {
            CurrentBaits.Clear();
            if (++NumCasts < 2 && World.Actors.Find(caster.TargetID) is var target && target != null)
                CurrentBaits.Add(new(caster, target, _shape));
        }
    }
}
