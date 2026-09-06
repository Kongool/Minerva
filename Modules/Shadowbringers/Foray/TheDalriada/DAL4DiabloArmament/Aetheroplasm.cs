// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL4DiabloArmament;

sealed class Aetheroplasm(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<Actor> orbs = [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.Aether)
        {
            orbs.Add(actor);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.Aetheroplasm or (uint)AID.FusionBurst)
        {
            orbs.Remove(caster);
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (orbs.Count != 0)
        {
            hints.Add("Soak the orbs!");
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = orbs.Count;
        if (count != 0)
        {
            var orbz = new ShapeDistance[count];
            hints.ActionsToExecute.Push(ActionID.MakeSpell(ClassShared.AID.Sprint), actor, ActionQueue.Priority.High);
            for (var i = 0; i < count; ++i)
            {
                var o = orbs[i];
                orbz[i] = new SDInvertedRect(o.Position + 0.5f * o.Rotation.ToDirection(), new WDir(default, 1f), 0.5f, 0.5f, 0.5f);
            }
            hints.AddForbiddenZone(new SDIntersection(orbz), World.FutureTime(5d));
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var count = orbs.Count;
        for (var i = 0; i < count; ++i)
        {
            Arena.ZoneCircleOutline(orbs[i].Position, 1.5f, Colors.Safe);
        }
    }
}
