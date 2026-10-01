// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Stormblood.ClassJobQuests.DRG;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 267)]
internal class DarkAsTheNightSky(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws).WithConnection(new Vector3(-338.28f, 69.61f, -384.66f))
    ];
}

