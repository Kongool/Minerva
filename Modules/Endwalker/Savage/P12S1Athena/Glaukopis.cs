// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P12S1Athena;

// TODO: not sure how exactly second target is selected, I think it is snapshotted to the current target when first cast happens? I can definitely taunt between casts without dying...
// TODO: consider generalizing...
sealed class Glaukopis(ModuleBase module) : Components.GenericBaitAway(module)
{
    private static readonly AOEShapeRect _shape = new(60f, 2.5f);

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (CurrentBaits.Any(b => b.Source.TargetID == b.Target.InstanceID) && actor.Role == Role.Tank)
            hints.Add(Module.PrimaryActor.TargetID != actor.InstanceID ? "Taunt!" : "Pass aggro!");
        base.AddHints(slot, actor, hints);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Glaukopis)
        {
            var target = World.Actors.Find(spell.TargetID);
            if (target != null)
                CurrentBaits.Add(new(caster, target, _shape));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.Glaukopis or (uint)AID.GlaukopisSecond)
        {
            CurrentBaits.Clear();
            if (++NumCasts < 2 && World.Actors.Find(caster.TargetID) is var target && target != null)
                CurrentBaits.Add(new(caster, target, _shape));
        }
    }
}
