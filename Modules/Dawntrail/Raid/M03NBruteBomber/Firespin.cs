// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M03NBruteBomber;

sealed class FireSpin(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private readonly AOEShapeCone cone = new(40f, 30f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.FireSpinCCW:
            case (uint)AID.InfernalSpinCCW:
                AddSequence(45f.Degrees());
                break;
            case (uint)AID.FireSpinCW:
            case (uint)AID.InfernalSpinCW:
                AddSequence(-45f.Degrees());
                break;
        }
        void AddSequence(Angle increment) => Sequences.Add(new(cone, spell.LocXZ, spell.Rotation, increment, Module.CastFinishAt(spell, 0.5d), 1f, 8));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.FireSpinFirst or (uint)AID.FireSpinRest or (uint)AID.InfernalSpinFirst or (uint)AID.InfernalSpinRest)
        {
            AdvanceSequence(0, World.CurrentTime);
        }
    }
}
