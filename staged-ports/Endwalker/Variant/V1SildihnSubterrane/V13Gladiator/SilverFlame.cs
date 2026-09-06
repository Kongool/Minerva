// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V13Gladiator;

sealed class SilverFlame(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private static readonly AOEShapeRect rect = new(60f, 5f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var increment = spell.Action.ID switch
        {
            (uint)AID.SilverFlameFirstCW => -10f.Degrees(),
            (uint)AID.SilverFlameFirstCCW => 10f.Degrees(),
            _ => default
        };
        if (increment != default)
        {
            Sequences.Add(new(rect, spell.LocXZ, spell.Rotation, increment, Module.CastFinishAt(spell), 2d, 5, 5, actorID: caster.InstanceID));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.SilverFlameFirstCCW or (uint)AID.SilverFlameFirstCW or (uint)AID.SilverFlameRest)
        {
            AdvanceSequence(caster.InstanceID, World.CurrentTime);
        }
    }
}
