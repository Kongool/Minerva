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
    /// Whether the walk to uptime runs at all, or the character only dodges (the user's calls, 2026-09-26 and -30):
    /// <list type="bullet">
    /// <item>Melee and tanks always close to reach, in a solo duty too: A Requiem for Heroes, 2026-09-30, a paladin
    /// run by Odysseus threw Shield Lob from across the arena ("im not moving to the boss"). Solo duties were
    /// dodge-only until then.</item>
    /// <item>Never for a character on the ranged band -- casters, physical ranged, healers, anyone whose role is
    /// unknown -- with nobody else in the party, nor in a solo duty.</item>
    /// </list>
    /// <para>Alone, every enemy is on the caster: the band's floor backs it away from a mob that follows, the walk
    /// in and out never ends, and each step cuts the cast the rotation just started. The Resonant, 2026-09-26: an
    /// Astrologian lost 17 of 74 casts, four of them started while the walk was under way. Melee and tanks keep
    /// theirs: their band is simply being in reach.</para>
    /// <para>Trust and Duty Support NPCs are a group: a tank of theirs holds the boss, and a caster that only dodged
    /// never walked into range of it -- The Porta Decumana, 2026-09-27, the ranged toons never engaged.</para>
    /// </summary>
    public static bool Walks(Role role, bool soloDuty, int groupMembers)
        => role is Role.Tank or Role.Melee || !soloDuty && groupMembers > 0;

    /// <summary>
    /// The role uptime is judged by while a role-play kit plays the fight: its reach, not the job underneath. Hien's
    /// kit is melee (3 yalms) whatever job you queued as; Y'shtola's is ranged (25). <paramref name="kitRange"/> 0 is no
    /// kit, and the job's role stands.
    /// <para>A Requiem for Heroes, 2026-09-30: with the kit pressing Hien's buttons, a solo duty's dodge-only rule left
    /// him wherever the fight put him, out of reach of Zenos. With a kit, the solo-duty rule steps aside
    /// (<see cref="Walks"/> is asked as though it were not one) and the kit's role decides: melee walks in, a ranged kit
    /// alone still only dodges.</para>
    /// </summary>
    public static Role KitRole(Role jobRole, float kitRange)
        => kitRange <= 0f ? jobRole : kitRange <= 5f ? Role.Melee : Role.Ranged;

    /// <summary>
    /// The side worth walking to for a positional: none while the enemy is attacking you. It turns to face its target, so
    /// the rear or flank you walk to swings round with you and the walk never ends; truly solo, that is every fight.
    /// With someone else holding it, the side stands.
    /// <para>The Will of the Moon, 2026-09-30: a ninja circled Sadu for 28.7 seconds ("need to not allow positionals when
    /// truely solo, all you do is spin around the mob").</para>
    /// </summary>
    public static Positional PositionalWorthWalking(Positional wanted, Actor target, ulong self)
        => target.TargetID == self ? Positional.Any : wanted;

    /// <summary>
    /// Whether this character has to start the fight itself: a boss module's target that nothing is fighting yet, with
    /// no other player in the party -- nobody, or only Trust and Duty Support NPCs, who wait for you to pull. Minerva
    /// then publishes the boss as the one to pull (<c>Minerva.Hints.PullTarget</c>); the rotation plugin targets it
    /// and opens from where the character stands, which for a caster is already in range of it.
    /// <para>The Porta Decumana, 2026-09-27: an Astrologian with Duty Support stood 23y from Ultima for 49 seconds,
    /// inside her cast range, and nothing happened until she was walked in by hand.</para>
    /// </summary>
    public static bool Pulls(bool hasModule, bool targetInCombat, int otherPlayers)
        => hasModule && !targetInCombat && otherPlayers == 0;

    /// <summary>How near an object to interact with the walk ends, past its hitbox, in yalms: inside the game's
    /// interact range, which the rotation plugin checks exactly before it clicks.</summary>
    public const float InteractReach = 2f;

    /// <summary>
    /// The walk to an object the fight needs clicked (<see cref="AIHints.InteractWithTarget"/>), or null for none. It
    /// replaces uptime while it lasts, as BossmodReborn's AI goes to the object before it clicks; danger still comes
    /// first. Not in a solo duty, which is played by hand and dodge-only (the user's call, 2026-09-26). Targetability is
    /// not asked: the module named the object, and an Empty Vessel spawns untargetable.
    /// </summary>
    public static UptimeGoal? InteractGoal(Actor? target, bool soloDuty)
        => soloDuty || target is not { IsDestroyed: false } t ? null : new UptimeGoal(t.Position, default, t.HitboxRadius + InteractReach);

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
