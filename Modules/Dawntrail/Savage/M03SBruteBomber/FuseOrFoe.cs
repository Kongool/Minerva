// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M03SBruteBomber;

sealed class InfernalSpin(ModuleBase module) : Components.GenericRotatingAOE(module)
{
    private static readonly AOEShapeCone _shape = new(40f, 30f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var increment = spell.Action.ID switch
        {
            (uint)AID.InfernalSpinFirstCW => -45f.Degrees(),
            (uint)AID.InfernalSpinFirstCCW => 45f.Degrees(),
            _ => default
        };
        if (increment != default)
        {
            Sequences.Add(new(_shape, spell.LocXZ, spell.Rotation, increment, Module.CastFinishAt(spell, 0.5f), 1.1f, 8));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.InfernalSpinFirstAOE or (uint)AID.InfernalSpinRestAOE)
            AdvanceSequence(0, World.CurrentTime);
    }
}

sealed class ExplosiveRain(ModuleBase module) : Components.ConcentricAOEs(module, _shapes)
{
    private static readonly AOEShape[] _shapes = [new AOEShapeCircle(8f), new AOEShapeDonut(8f, 16f), new AOEShapeDonut(16f, 24f)];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.ExplosiveRain11 or (uint)AID.ExplosiveRain21)
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        var order = spell.Action.ID switch
        {
            (uint)AID.ExplosiveRain11 or (uint)AID.ExplosiveRain21 => 0,
            (uint)AID.ExplosiveRain12 or (uint)AID.ExplosiveRain22 => 1,
            (uint)AID.ExplosiveRain13 or (uint)AID.ExplosiveRain23 => 2,
            _ => -1
        };
        if (!AdvanceSequence(order, spell.LocXZ, World.FutureTime(4d)))
            ReportError($"Unexpected ring {order}");
    }
}
