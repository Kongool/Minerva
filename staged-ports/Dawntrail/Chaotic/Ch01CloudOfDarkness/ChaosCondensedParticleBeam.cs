// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class ChaosCondensedParticleBeam(ModuleBase module) : Components.GenericWildCharge(module, 3f, default, 50f)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ChaosCondensedParticleBeam)
        {
            Source = caster;
            Activation = Module.CastFinishAt(spell, 0.7d);
            foreach (var (i, p) in Raid.WithSlot(true, false, true))
                PlayerRoles[i] = p.Role == Role.Tank ? PlayerRole.Target : PlayerRole.ShareNotFirst;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ChaosCondensedParticleBeamAOE1 or (uint)AID.ChaosCondensedParticleBeamAOE2)
        {
            ++NumCasts;
            Source = null;
        }
    }
}
