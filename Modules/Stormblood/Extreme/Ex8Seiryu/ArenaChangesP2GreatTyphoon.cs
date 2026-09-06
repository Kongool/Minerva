// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex8Seiryu;

sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    public override void DrawArenaBackground(int pcSlot, Actor pc)
    {
        if (!pc.Position.InCircle(Center, 20f))
        {
            Bounds = Trial.T09Seiryu.Seiryu.Phase2WaterBounds;
        }
        else
        {
            Bounds = Trial.T09Seiryu.Seiryu.Phase2Bounds;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Bounds.Radius > 20f)
        {
            hints.AddForbiddenZone(new SDInvertedCircle(Center, 19f), DateTime.MaxValue);
        }
    }
}

sealed class GreatTyphoonCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GreatTyphoonCone, new AOEShapeDonutSector(20f, 45f, 15f.Degrees()))
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (Bounds.Radius > 20f)
            return base.ActiveAOEs(slot, actor);
        else
            return [];
    }
}

sealed class GreatTyphoonDonut(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeDonut donut1 = new(20f, 28f), donut2 = new(26f, 34f), donut3 = new(32f, 40f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => Bounds.Radius > 20f ? _aoe : [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.GreatTyphoon1 => donut1,
            (uint)AID.GreatTyphoon2 => donut2,
            (uint)AID.GreatTyphoon3 => donut3,
            _ => null
        };
        if (shape != null)
        {
            _aoe = [new(shape, spell.LocXZ, default, Module.CastFinishAt(spell), actorID: caster.InstanceID)];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.GreatTyphoon1 or (uint)AID.GreatTyphoon2 or (uint)AID.GreatTyphoon3)
        {
            ref var aoe = ref _aoe[0];
            if (caster.InstanceID == aoe.ActorID) // depending on latency a new cast can start in the same frame as previous ended
            {
                _aoe = [];
            }
        }
    }
}
