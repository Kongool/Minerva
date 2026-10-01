// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_questbattle.py.
using System;
using System.Collections.Generic;
using System.Linq;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.ARealmReborn.ClassJobQuests.MNK;

[ZoneModuleInfo(BossModuleInfo.Maturity.Contributed, 323)]
internal class ReturnOfTheHolyfist(WorldState ws) : QuestBattle(ws)
{
    public override void AddQuestAIHints(Actor player, AIHints hints) => hints.PrioritizeAll();
}
