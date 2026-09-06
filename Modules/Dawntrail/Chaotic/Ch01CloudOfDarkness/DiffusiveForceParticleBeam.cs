// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

// note: it seems that normally first wave (radius 7) hits people inside, and second wave (radius 5) hits people outside
// however, if some of the players in the mid are dead, some players on the outside will be hit by first wave (up to a total of 12 hits)
// if there are <= 12 players alive, everyone will be hit by the first wave, and the second wave will never happen
// so for safety we just show larger radius around everyone
// TODO: show second wave for players not hit by first wave
sealed class DiffusiveForceParticleBeam(ModuleBase module) : Components.UniformStackSpread(module, default, 7f)
{
    public int NumCasts;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DiffusiveForceParticleBeam)
            AddSpreads(Raid.WithoutSlot(true, false, true), Module.CastFinishAt(spell, 0.7d));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.DiffusiveForceParticleBeamAOE1 or (uint)AID.DiffusiveForceParticleBeamAOE2)
        {
            ++NumCasts;
            Spreads.RemoveAll(s => s.Target.InstanceID == spell.MainTargetID);
        }
    }
}
