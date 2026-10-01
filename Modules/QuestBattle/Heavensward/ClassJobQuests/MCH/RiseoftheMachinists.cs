// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.MCH;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 426)]
internal class RiseOfTheMachinists(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .With(obj => {
                Actor? tedal = null;
                obj.OnActorCreated += (act) => {
                    if (act.OID == 0x1158) { tedal ??= act; } };

                obj.AddAIHints += (player, hints) => {
                    foreach(var h in hints.PotentialTargets) { if (h.Actor.TargetID == tedal?.InstanceID) { h.Priority = 5; } } };
            })
        ];
}

