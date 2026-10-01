// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.PLD;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 435)]
internal class BigSolleretsToFill(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws).Hints((player, hints) => hints.PrioritizeTargetsByOID(0x11D2, 5))
    ];
}

