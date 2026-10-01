// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Stormblood.ClassJobQuests.WAR;

// TODO: is this needed
[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 260)]
internal class CuriousGorgeMeetsHisMatch(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
    ];
}

