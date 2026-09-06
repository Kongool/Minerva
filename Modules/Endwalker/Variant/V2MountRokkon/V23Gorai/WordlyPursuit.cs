// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V23Gorai;

sealed class WorldlyPursuit(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private static readonly AOEShapeCross cross = new(60f, 10f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var increment = spell.Action.ID switch
        {
            (uint)AID.WorldlyPursuitFirstCW => -22.5f.Degrees(),
            (uint)AID.WorldlyPursuitFirstCCW => 22.5f.Degrees(),
            _ => default
        };
        if (increment != default)
        {
            ImminentColor = Colors.AOE;
            Sequences.Add(new(cross, spell.LocXZ, spell.Rotation, increment, Module.CastFinishAt(spell), 3.7d, 5, 1));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WorldlyPursuitFirstCCW or (uint)AID.WorldlyPursuitFirstCW or (uint)AID.WorldlyPursuitRest)
        {
            AdvanceSequence(0, World.CurrentTime);
        }
    }
}
