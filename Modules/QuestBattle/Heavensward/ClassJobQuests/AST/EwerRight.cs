using System.Collections.Generic;
using Minerva;
using Minerva.QuestBattle;

namespace Minerva.QuestBattle.Heavensward.ClassJobQuests.AST;

/// <summary>
/// Ewer Right (quest 2017), from Saar's hand-played run on 2026-10-05; BossmodReborn has no script for it. One fight
/// where the duty starts: Rathefrost Bandits in waves, then the Arcanima Marionette and the Bandit Leader. Nothing to
/// walk to and nothing to click. The work is keeping Quimperain Evertrue and the Ishgardian Astrologian alive (they
/// fell to 17% and 10% with 21 heals by hand), which <see cref="AllyHeals"/> does for any quest battle; this script
/// exists so the duty is played as one.
/// </summary>
[ZoneModuleInfo(BossModuleInfo.Maturity.WIP, 417)]
internal class EwerRight(WorldState ws) : QuestBattle(ws)
{
    public override List<QuestObjective> DefineObjectives(WorldState ws) => [];
}
