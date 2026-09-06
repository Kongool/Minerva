// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A13ArkAngels;

sealed class DecisiveBattle(ModuleBase module) : ModuleComponent(module)
{
    public readonly Actor?[] AssignedBoss = new Actor?[PartyState.MaxAllianceSize];
    private readonly A13ArkAngels bossmod = (A13ArkAngels)module;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (slot < PartyState.MaxAllianceSize && AssignedBoss[slot] is var assignedSlot && assignedSlot != null && World.Actors.Find(actor.TargetID) is Actor target)
        {
            if (target != assignedSlot && target.OID is (uint)OID.BossMR or (uint)OID.BossTT or (uint)OID.BossGK)
            {
                hints.Add($"Target {assignedSlot?.Name}!");
            }
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.DecisiveBattle && Raid.FindSlot(source.InstanceID) is var slot && slot >= 0)
        {
            AssignedBoss[slot] = World.Actors.Find(tether.Target);
        }
    }

    // fall back since players outside arena bounds do not get tethered but will still receive status effects
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var boss = status.ID switch
        {
            (uint)SID.EpicHero => bossmod.BossMR(),
            (uint)SID.VauntedHero => bossmod.BossTT(),
            (uint)SID.FatedHero => Module.PrimaryActor,
            _ => null
        };
        if (boss != null && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            AssignedBoss[slot] = boss;
        }
    }

    // if player joins fight late, statemachine won't reset this component properly
    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.EpicVillain)
        {
            Array.Clear(AssignedBoss);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (slot < PartyState.MaxAllianceSize && AssignedBoss[slot] is var assignedSlot && assignedSlot != null)
        {
            var count = hints.PotentialTargets.Count;
            for (var i = 0; i < count; ++i)
            {
                var enemy = hints.PotentialTargets[i];
                if (enemy.Actor != assignedSlot)
                {
                    enemy.Priority = AIHints.Enemy.PriorityInvincible;
                }
            }
        }
    }
}
