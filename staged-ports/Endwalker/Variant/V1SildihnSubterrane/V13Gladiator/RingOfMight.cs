// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V13Gladiator;

abstract class RingOfMight(ModuleBase module, AOEShape[] shapes, double riskyWithSecondsLeft) : Components.ConcentricAOEs(module, shapes, riskyWithSecondsLeft: riskyWithSecondsLeft)
{
    private readonly SunderedRemains _aoe = module.FindComponent<SunderedRemains>()!;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe.Casters.Count == 0 ? base.ActiveAOEs(slot, actor) : [];

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        // extra ai hint: stay close to the edge of the first aoe
        if (Sequences.Count != 0)
        {
            ref var s = ref Sequences.Ref(0);
            if (s.NumCastsDone == 0 && Shapes[1] is AOEShapeDonut donut)
            {
                hints.AddForbiddenZone(new SDInvertedCircle(s.Origin, donut.InnerRadius + 2f), s.NextActivation);
            }
        }
    }
}

sealed class RingOfMight1(ModuleBase module) : RingOfMight(module, [new AOEShapeCircle(8f), new AOEShapeDonut(8f, 30f)], riskyWithSecondsLeft: 3d)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RingOfMight1Out)
        {
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (Sequences.Count != 0)
        {
            var order = spell.Action.ID switch
            {
                (uint)AID.RingOfMight1Out => 0,
                (uint)AID.RingOfMight1In => 1,
                _ => -1
            };
            AdvanceSequence(order, spell.LocXZ, World.FutureTime(2d));
        }
    }
}

sealed class RingOfMight2(ModuleBase module) : RingOfMight(module, [new AOEShapeCircle(13f), new AOEShapeDonut(13f, 30f)], riskyWithSecondsLeft: 4d)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RingOfMight2Out)
        {
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (Sequences.Count != 0)
        {
            var order = spell.Action.ID switch
            {
                (uint)AID.RingOfMight2Out => 0,
                (uint)AID.RingOfMight2In => 1,
                _ => -1
            };
            AdvanceSequence(order, spell.LocXZ, World.FutureTime(2d));
        }
    }
}

sealed class RingOfMight3(ModuleBase module) : RingOfMight(module, [new AOEShapeCircle(18f), new AOEShapeDonut(18f, 30f)], riskyWithSecondsLeft: 5d)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.RingOfMight3Out)
        {
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (Sequences.Count != 0)
        {
            var order = spell.Action.ID switch
            {
                (uint)AID.RingOfMight3Out => 0,
                (uint)AID.RingOfMight3In => 1,
                _ => -1
            };
            AdvanceSequence(order, spell.LocXZ, World.FutureTime(2d));
        }
    }
}
