namespace Minerva.QuestBattle;

/// <summary>
/// A healer keeps the duty's allied NPCs alive while a script plays a solo duty. Slings and Arrows, Saar 2026-10-05: the
/// Pirate Captain took Quimperain from 98% to dead in 34 seconds while Saar, on Astrologian, cast Malefic at the Serpents,
/// and the duty fails when he dies. BossmodReborn's healer AI heals the NPCs it keeps in party slots, which is why its
/// scripts never say so; Daedalus heals the party, and solo that is the player alone.
/// <para>Asked of the rotation plugin as an action (<see cref="AIHints.ActionsToExecute"/>, published as
/// <c>Minerva.Hints.RoleplayActions</c>): the class's single-target heal that every healer of it already has.
/// Not while playing a role-play kit, whose own rotation heals with the kit's buttons.</para>
/// </summary>
public static class AllyHeals
{
    /// <summary>An ally below this share of its HP is healed.</summary>
    public const float Below = 0.6f;

    /// <summary>The heals' range: an ally further off is out of reach, and asking would only stall the rotation.</summary>
    public const float Range = 30f;

    public static uint BasicHeal(Class c) => c switch
    {
        Class.CNJ or Class.WHM => (uint)WHM.AID.Cure,
        Class.SCH => (uint)SCH.AID.Physick,
        Class.AST => (uint)AST.AID.BeneficII, // the job starts at 30, past Benefic II's 26
        Class.SGE => (uint)SGE.AID.Diagnosis,
        _ => 0u,
    };

    /// <summary>Ask for the class's heal on the lowest allied NPC under <see cref="Below"/>; the one asked for, or null.</summary>
    public static Actor? Request(WorldState ws, Actor player, AIHints hints)
    {
        var heal = BasicHeal(player.Class);
        if (heal == 0 || player.FindStatus((uint)Roleplay.SID.RolePlaying) != null)
            return null;
        Actor? lowest = null;
        foreach (var a in ws.PartyAndAllies())
        {
            if (a.Type == ActorType.Player || !a.IsTargetable || a.HPRatio >= Below || (a.Position - player.Position).LengthSq() > Range * Range)
                continue;
            if (lowest == null || a.HPRatio < lowest.HPRatio)
                lowest = a;
        }
        if (lowest != null)
            hints.ActionsToExecute.Push(ActionID.MakeSpell(heal), lowest, ActionQueue.Priority.High);
        return lowest;
    }
}
