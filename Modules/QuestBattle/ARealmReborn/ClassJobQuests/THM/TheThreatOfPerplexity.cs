// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.ARealmReborn.ClassJobQuests.THM;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 325)]
internal class TheThreatOfPerplexity(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .Hints((player, hints) => hints.PrioritizeTargetsByOID(0x2A4, 0x2A5))
    ];
}
