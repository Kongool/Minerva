// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.AST;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 413)]
internal class SpearheadingInitiatives(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .WithConnection(new Vector3(297.23f, 308.81f, -425.09f))
            .CompleteOnKilled(0x11FA),
        new QuestObjective(ws)
            .WithConnection(new Vector3(215.93f, 327.43f, -456.19f))
            .CompleteOnKilled(0x11F9),
        new QuestObjective(ws)
            .WithConnection(new Vector3(229.05f, 347.18f, -513.37f))
            .CompleteOnKilled(0x11FA),
        new QuestObjective(ws)
            .WithConnection(new Vector3(268.09f, 362.50f, -605.65f))
            .With(obj => {
                obj.OnConditionChange += (flag, value) => obj.CompleteIf(flag == ConditionFlag.BetweenAreas && !value);
            }),
        new QuestObjective(ws)
            .WithConnection(new Vector3(262.16f, 359.18f, -679.22f))
            .WithInteract(0x1E9A80)
    ];

    // The False Temple troops march in from the start, targetable, and each objective sends one off to fight a mob; the
    // objective ends on its death, and the rest of the column despawns at the cutscene without ever fighting. The user,
    // 2026-10-05: kill the trooper when it goes after a mob, then the mob it was fighting; killing the mob first makes
    // the trooper reset. Busy with a mob, a trooper is not in a fight with us, so nothing stopped for one, and the route
    // walked on past it. Marching troopers are left alone: chasing the column only drags the character off the route.
    private static readonly uint[] Troops = [0x11F8, 0x11F9, 0x11FA, 0x11FE, 0x11FF, 0x1200, 0x1203, 0x121A];
    private static readonly uint[] Wildlife = [0x11FB, 0x11FC, 0x13A5, 0x13A6]; // Redhorn Ogre, Downy Aevis, Ice Sprite, Ornery Karakul

    public override void AddQuestAIHints(Actor player, AIHints hints)
    {
        static bool Near(Actor a, Actor player) => (a.Position - player.Position).LengthSq() <= TargetRange * TargetRange;
        var trooperFighting = false;
        foreach (var e in hints.PotentialTargets)
            trooperFighting |= e.Actor.InCombat && Array.IndexOf(Troops, e.Actor.OID) >= 0 && Near(e.Actor, player);
        var inReach = trooperFighting;
        foreach (var e in hints.PotentialTargets)
        {
            if (!e.Actor.InCombat)
                continue;
            if (Array.IndexOf(Troops, e.Actor.OID) >= 0)
                e.Priority = 2;
            else if (Array.IndexOf(Wildlife, e.Actor.OID) >= 0)
            {
                // left alone while a trooper is fighting nearby, so the trooper keeps something to fight and stays
                e.Priority = trooperFighting ? AIHints.Enemy.PriorityForbidden : 1;
                inReach |= Near(e.Actor, player);
            }
        }
        // the route waits while there is one to kill in reach, and carries on once they are dead
        if (inReach)
            hints.QuestTravel = null;
    }
}

