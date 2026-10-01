// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.ARealmReborn.ClassJobQuests.THM;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 324)]
internal class TheThreatOfSuperiority(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .Hints((player, hints) => hints.PrioritizeAll())
            .WithConnection(new Vector3(100.94f, -24.09f, 257.01f))
            .WithInteract(0x1E8A3F)
    ];
}
