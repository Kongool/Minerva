// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Stormblood.ClassJobQuests.BLM;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 444)]
internal class ShadesOfShatotto(WorldState ws) : QuestBattle(ws)
{
    public override void AddQuestAIHints(Actor player, AIHints hints)
    {
        foreach (var h in hints.PotentialTargets)
        {
            h.Priority = h.Actor.OID == 0x1AE8 ? 0 : 1;
        }
    }
}

