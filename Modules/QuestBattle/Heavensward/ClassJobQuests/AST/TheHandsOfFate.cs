// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.AST;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 414)]
internal class TheHandsOfFate(WorldState ws) : QuestBattle(ws)
{
    public override void AddQuestAIHints(Actor player, AIHints hints)
    {
        foreach (var h in hints.PotentialTargets)
        {
            if (h.Actor.OID is >= 0x1268 and <= 0x126D)
            {
                h.Priority = 5;
            }
        }
    }
}

