namespace Minerva;

/// <summary>
/// What the walk back to uptime aims at. Split out of the plugin's AIManager so the choice can be tested: all of
/// it is decided from the hints and the actors, except whether a target stands on the player's own floor, which
/// needs the game's collision and is handed in as <c>reachable</c>.
/// </summary>
public static class UptimeTargeting
{
    /// <summary>
    /// The enemy to regain uptime on, before the in-the-fight leash is applied, or null for none.
    ///
    /// <para>In order: a target the module forces; the module's highest priority; the player's own target, when
    /// <paramref name="followOwnTarget"/> is on and that target is neither forbidden nor on another floor; the
    /// module's boss, unless forbidden; the player's own target regardless; and in trash with no module, whatever
    /// is already fighting us. <paramref name="prioritised"/> and <paramref name="engaged"/> are only called when
    /// their turn comes, because each remembers its pick to break ties next frame.</para>
    /// </summary>
    public static Actor? Choose(
        AIHints hints,
        Actor? ownTarget,
        Actor? primary,
        bool hasModule,
        bool followOwnTarget,
        Func<Actor?> prioritised,
        Func<Actor, bool> reachable,
        Func<Actor?> engaged)
        => hints.ForcedTarget is { IsDeadOrDestroyed: false } forced ? forced
            : prioritised()
            // The rotation's target, ahead of the boss: when Daedalus retargets onto an add, the add is where
            // uptime is. Forbidden targets are skipped, so an invincible boss the player still has targeted
            // falls through to the lines below exactly as before.
            ?? (followOwnTarget && IsHostile(ownTarget) && !hints.IsForbiddenTarget(ownTarget!) && reachable(ownTarget!) ? ownTarget
            : primary is { IsDeadOrDestroyed: false } boss && !hints.IsForbiddenTarget(boss) ? boss
            : IsHostile(ownTarget) ? ownTarget
            // Trash, with nothing targeted: follow whatever is already fighting us, as BossmodReborn's AI does. Only
            // reached without a module and without a target of one's own -- a boss fight keys on its primary actor,
            // and anybody with a target keeps it.
            : !hasModule ? engaged()
            : null);

    /// <summary>
    /// The point on <paramref name="target"/>'s hitbox edge nearest <paramref name="from"/>, which is what the
    /// floor test walks to -- or null when that edge is already within a yalm, so there is nothing to test.
    /// <para>The edge and not the centre, because a huge boss (Shinryu is R22, its wings R15) often has its centre
    /// hanging over the void while its edge is well inside the arena.</para>
    /// </summary>
    public static WPos? HitboxEdge(WPos from, Actor target)
    {
        var to = target.Position - from;
        var reach = MathF.Max(to.Length() - target.HitboxRadius, 0f);
        return reach < 1f ? null : from + (to.Normalized() * reach);
    }

    private static bool IsHostile(Actor? a) => a is { IsDeadOrDestroyed: false, Type: ActorType.Enemy, IsAlly: false };
}
