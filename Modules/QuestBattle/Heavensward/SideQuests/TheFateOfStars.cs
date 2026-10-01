// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.SideQuests;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 173)]
internal class ABloodyReunion(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .WithConnection(new Vector3(288.62f, 222.20f, 271.82f))
            .Hints((player, hints) => {
                if (World.Actors.FirstOrDefault(x => x.OID == 0x1EA080u && x.EventState != 7) is Actor shield)
                {
                    hints.AddForbiddenZone(new SDInvertedCircle(shield.Position, 5f));
                }
            })
    ];
}
