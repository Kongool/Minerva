// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.MCH;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 424)]
internal class SecuringTheLocks(WorldState ws) : QuestBattle(ws)
{
    private static readonly WPos Center = new(231.29f, 124.36f);

    public override void AddQuestAIHints(Actor player, AIHints hints)
    {
        hints.Center = Center;
        hints.PathfindMapBounds = new ArenaBoundsCircle(120);

        if (player.TargetID == 0)
        {
            var closest = hints.PotentialTargets.MinBy(p => p.Actor.DistanceToHitbox(player));
            if (closest != null)
            {
                hints.GoalZones.Add(AIHints.GoalSingleTarget(closest.Actor, 25));
            }
            else
            {
                hints.GoalZones.Add(AIHints.GoalSingleTarget(Center, 5));
            }
        }
    }
}

