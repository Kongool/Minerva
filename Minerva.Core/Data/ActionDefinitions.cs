namespace Minerva;

/// <summary>
/// Action ids that boss modules name directly, mirroring BossmodReborn's <c>ActionDefinitions</c>.
///
/// <para>These are the role and general actions a module asks for by name — almost always the two
/// knockback immunities before a knockback, occasionally Sprint to cross an arena in time. A module
/// pushing one of these onto <see cref="AIHints.ActionsToExecute"/> is stating a fact about the fight:
/// "this knockback is survivable by pressing Arm's Length here". That is worth carrying even though
/// Minerva does not press buttons — it never has and is not going to, since deciding what to press is
/// the rotation plugin's job.</para>
///
/// <para>Deliberately not a port of BossmodReborn's full action database. That file exists to drive an
/// autorotation, which Minerva does not have; only the ids modules actually reference are here, so the
/// list stays something a reader can check rather than a table nobody maintains.</para>
/// </summary>
public static class ActionDefinitions
{
    /// <summary>Arm's Length — knockback immunity, role action.</summary>
    public static readonly ActionID Armslength = new(ActionType.Spell, 7548u);

    /// <summary>Surecast — knockback immunity, the caster's equivalent.</summary>
    public static readonly ActionID Surecast = new(ActionType.Spell, 7559u);

    /// <summary>Sprint.</summary>
    public static readonly ActionID IDSprint = new(ActionType.Spell, 3u);

    /// <summary>Pilgrim's Potion, used by the Occult Crescent auto-potion module.</summary>
    public static readonly ActionID IDPotionPilgrim = new(ActionType.Item, 47102u);
}
