// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class ActivePivotParticleBeam(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private static readonly AOEShapeRect _shape = new(40f, 9f, 40f);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var rotation = spell.Action.ID switch
        {
            (uint)AID.ActivePivotParticleBeamCW => -22.5f.Degrees(),
            (uint)AID.ActivePivotParticleBeamCCW => 22.5f.Degrees(),
            _ => default
        };
        if (rotation != default)
        {
            Sequences.Add(new(_shape, spell.LocXZ, spell.Rotation, rotation, Module.CastFinishAt(spell, 0.6d), 1.6d, 5));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ActivePivotParticleBeamAOE)
        {
            AdvanceSequence(0, World.CurrentTime);
        }
    }
}
