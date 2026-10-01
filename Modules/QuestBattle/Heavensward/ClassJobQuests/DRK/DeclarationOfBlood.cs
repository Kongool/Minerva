// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.DRK;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 432)]
internal class DeclarationOfBlood(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws).WithConnection(new Vector3(-142.37f, 3.20f, 677.68f)).WithInteract(0x1E9D44)
    ];
}

