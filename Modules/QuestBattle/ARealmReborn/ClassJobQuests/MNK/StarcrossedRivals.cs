// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.ARealmReborn.ClassJobQuests.MNK;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 321)]
internal class StarcrossedRivals(WorldState ws) : QuestBattle(ws)
{
    public override void AddQuestAIHints(Actor player, AIHints hints)
    {
        foreach (var h in hints.PotentialTargets)
        {
            h.Priority = h.Actor.OID switch
            {
                0x2BD or 0x2BE => 2,
                0x2BA => 0,
                _ => 1
            };
        }
    }
}
