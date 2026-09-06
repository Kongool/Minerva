// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Extreme.FTME1TwoHeadedAevis;

[SkipLocalsInit]
sealed class Buffet(ModuleBase module) : ModuleComponent(module)
{
    private readonly Actor?[] AssignedBoss = new Actor?[PartyState.MaxPartySize];
    private readonly FTME1TwoHeadedAevis bossModule = (FTME1TwoHeadedAevis)module;
    //private readonly FTME1TwoHeadedAevisConfig _config = Service.Config.Get<FTME1TwoHeadedAevisConfig>();

    public Actor? GetAssignedBoss(int slot)
    {
        return AssignedBoss[slot];
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.Buffet && Raid.FindSlot(source.InstanceID) is var slot && slot >= 0)
        {
            AssignedBoss[slot] = World.Actors.Find(tether.Target);
        }
    }

    // fall back since players outside arena bounds do not get tethered but will still receive status effects
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var boss = status.ID switch
        {
            (uint)SID.EpicHero => bossModule.GreenHead(),
            (uint)SID.FatedHero => bossModule.BlueHead(),
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
        if (status.ID is (uint)SID.EpicVillain or (uint)SID.FatedVillain)
        {
            Array.Clear(AssignedBoss);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (slot < PartyState.MaxPartySize && AssignedBoss[slot] is var assignedSlot && assignedSlot != null && World.Actors.Find(actor.TargetID) is Actor target)
        {
            if (target != assignedSlot)
            {
                hints.Add($"Target {assignedSlot?.Name}!");
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (slot < PartyState.MaxAllianceSize && AssignedBoss[slot] is var assignedSlot && World.Actors.Find(actor.TargetID) is Actor target)
        {
            if (assignedSlot != null)
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
                /*
                // also ignore forced targeting if current target is a PC
                if (_config.ForceTargeting && (target == null || target.Type != ActorType.Player) && target != assignedSlot)
                {
                    hints.ForcedTarget = assignedSlot;
                }
                */
            }
            else
            {
                var count = hints.PotentialTargets.Count;
                for (var i = 0; i < count; ++i)
                {
                    var enemy = hints.PotentialTargets[i];
                    if (enemy.Actor.HPMP.CurHP == 1u)
                    {
                        enemy.Priority = AIHints.Enemy.PriorityPointless;
                    }
                    /*
                    else if (_config.ForceTargeting && (target == null || target.Type != ActorType.Player))
                    {
                        hints.ForcedTarget = enemy?.Actor;
                    }
                    */
                }
            }
        }
    }
}
