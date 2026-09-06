// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V11Geryon;

sealed class Gigantomill(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private readonly AOEShapeCross cross = new(72f, 5f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var increment = spell.Action.ID switch
        {
            (uint)AID.GigantomillFirstCW => -22.5f.Degrees(),
            (uint)AID.GigantomillFirstCCW => 22.5f.Degrees(),
            _ => default
        };
        if (increment != default)
        {
            Sequences.Add(new(cross, spell.LocXZ, spell.Rotation, increment, Module.CastFinishAt(spell), 1.7d, 5, 2));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.GigantomillFirstCW or (uint)AID.GigantomillFirstCCW or (uint)AID.GigantomillRest)
        {
            AdvanceSequence(0, World.CurrentTime);
        }
    }
}
