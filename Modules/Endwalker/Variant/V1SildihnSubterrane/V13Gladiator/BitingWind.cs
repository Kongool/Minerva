// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V13Gladiator;

sealed class BitingWindUpdraft(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BitingWindUpdraft, 6f);

sealed class BitingWindSmall(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 4f, (uint)AID.BitingWindAOE, GetVoidzones, 0.3d)
{
    private static List<Actor> GetVoidzones(ModuleBase module) => module.Enemies((uint)OID.WhirlwindSmall);
}

sealed class BitingWindUpdraftVoidzone(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private bool active;
    private static readonly AOEShapeCircle circle = new(6f), circleInverted = new(6f, true);
    private DateTime activation = DateTime.MaxValue;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (!active)
        {
            return [];
        }

        if (World.CurrentTime > activation)
        {
            ref var aoe = ref _aoe[0];
            aoe.Color = Colors.SafeFromAOE;
            aoe.Shape = circleInverted;
        }
        return _aoe;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (active && spell.Action.ID == (uint)AID.ShatteringSteel)
        {
            activation = Module.CastFinishAt(spell, -5d);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.BitingWindUpdraft)
        {
            _aoe = [new(circle, spell.LocXZ)];
            active = true;
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == (uint)OID.WhirlwindUpdraft)
        {
            active = false;
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!active)
        {
            return;
        }

        if (activation != DateTime.MaxValue)
        {
            hints.Add(World.CurrentTime < activation ? "Prepare to walk into updraft!" : "Walk into updraft!");
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (!active)
        {
            return;
        }
        base.AddAIHints(slot, actor, assignment, hints);

        // stay close to updraft
        if (activation != DateTime.MaxValue && World.CurrentTime < activation)
        {
            ref var aoe = ref _aoe[0];
            hints.AddForbiddenZone(new SDInvertedCircle(aoe.Origin, 12f), activation);
        }
    }
}
