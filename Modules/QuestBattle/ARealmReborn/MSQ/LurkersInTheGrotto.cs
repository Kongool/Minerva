// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.ARealmReborn.MSQ;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 332)]
public class LurkersInTheGrotto(WorldState ws) : QuestBattle(ws)
{
    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        foreach (var p in hints.PotentialTargets)
        {
            p.Priority = p.Actor.OID == 0x386 ? 0 : 1;
        }
    }
}
