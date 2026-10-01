// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Shadowbringers.MSQ;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 662)]
internal class TheOracleOfLight(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [
        new QuestObjective(ws)
            .WithConnection(new Vector3(260.10f, 132.79f, -182.20f))
            .WithConnection(new Vector3(224.17f, 134.83f, -265.39f))
            .WithConnection(new Vector3(134.58f, 134.83f, -322.44f))
            .WithConnection(new Vector3(51.25f, 134.83f, -368.62f))
            .WithConnection(new Vector3(-33.62f, 161.07f, -362.15f))
            .WithInteract(0x1EADB9)
            .CompleteOnDestroyed(0x1EADB9),

        new QuestObjective(ws).WithConnection(new Vector3(123.42f, 134.83f, -314.19f))
    ];
}
