// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.ARealmReborn.ClassJobQuests.PLD;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 362)]
internal class KeepingTheOath(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws).CompleteOnKilled(0x617),
        new QuestObjective(ws)
            .With(obj => {
                obj.OnConditionChange += (cond, value) => obj.CompleteIf(cond == ConditionFlag.BetweenAreas && !value);
            }),
        new QuestObjective(ws)
            .WithConnection(new Vector3(-878.08f, 227.54f, -4.26f))
            .WithInteract(0x1E8D78)
            .With(obj => {
                obj.OnActorCreated += (act) => obj.CompleteIf(act.OID == 0x899);
            }),
        new QuestObjective(ws)
            .Hints((player, hints) => hints.PrioritizeTargetsByOID(0x8B5, 5))
    ];
}
