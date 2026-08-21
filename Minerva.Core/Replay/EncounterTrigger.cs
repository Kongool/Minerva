namespace Minerva;

/// <summary>
/// Decides whether the player is in a fight worth recording, when no module claims it.
/// <para>Lives here rather than beside the recorder because it is pure: three facts about actors and one
/// distance, with no game and no plugin state. That makes it testable, which it needs to be — every
/// condition it drops is a false start the user only finds out about from a spurious recording.</para>
/// </summary>
public static class EncounterTrigger
{
    /// <summary>How much bigger than the local player an enemy must be to count as a boss worth recording.
    /// A ratio rather than a flat HP number so it holds across level ranges; generous enough that trash,
    /// adds and ambient open-world mobs stay out of the corpus.</summary>
    public const float BossHPMultiple = 10f;

    /// <summary>
    /// How close that enemy has to be to count as the fight this player is in.
    /// <para>Comfortably larger than any single arena and far smaller than a zone. An Occult Crescent
    /// critical encounter reported by the zone's event director is running somewhere; that says nothing
    /// about whether this player is in it, and standing in base camp is not participation.</para>
    /// </summary>
    public const float MaxDistance = 50f;

    /// <summary>
    /// The enemy the player is fighting, or null. All three conditions have to hold at once — big, near,
    /// and mutually in combat — because each on its own is satisfied constantly in an open-world zone.
    /// </summary>
    public static Actor? NearbyEngagedBoss(Actor? player, IEnumerable<Actor> actors)
    {
        if (player == null || player.HPMP.MaxHP == 0 || !player.InCombat)
            return null;

        var floor = player.HPMP.MaxHP * BossHPMultiple;
        var reachSq = MaxDistance * MaxDistance;
        foreach (var a in actors)
        {
            if (a.IsAlly || !a.InCombat || !a.IsTargetable || a.IsDeadOrDestroyed)
                continue;
            if (a.HPMP.CurHP == 0 || a.HPMP.MaxHP < floor)
                continue;
            if ((a.Position - player.Position).LengthSq() > reachSq)
                continue;
            return a;
        }

        return null;
    }
}
