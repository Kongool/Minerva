// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.ARealmReborn.ClassJobQuests.LNC;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 307)]
internal class ADangerousProposition(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .WithConnection(new Vector3(304.20f, -1.18f, -285.26f))
            .Hints((player, hints) => hints.PrioritizeAll())
            .CompleteOnKilled(0x21F)
    ];
}
