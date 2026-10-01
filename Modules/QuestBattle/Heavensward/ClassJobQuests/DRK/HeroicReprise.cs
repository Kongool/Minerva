// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.DRK;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 431)]
internal class HeroicReprise(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .WithConnection(new Vector3(-74.51f, -24.25f, 269.76f))
            .WithInteract(0x1E9B3F)
            .With(obj => {
                obj.OnActorEventStateChanged += (act) => obj.CompleteIf(act.OID == 0x1274 && act.EventState == 0);
            }),

        new QuestObjective(ws)
            .WithConnection(new Vector3(-124.39f, -30.14f, 302.91f))
    ];
}

