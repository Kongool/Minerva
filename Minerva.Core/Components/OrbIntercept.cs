namespace Minerva.Components;

/// <summary>
/// "Stand in front of the orb": a moving orb has to be body-blocked before it reaches its target, so the
/// only acceptable ground is the stretch of its path just ahead of it.
/// <para>BossmodReborn says this with a one-yalm box in front of each orb and an activation of
/// <see cref="DateTime.MaxValue"/>. Neither survives Minerva's solver. A zone activating at MaxValue is past
/// every deadline, so the imminent-danger test never looks at it and the character simply holds uptime.
/// And the deepest point of a one-yalm box is half a yalm from its edge, inside the one-yalm safety margin,
/// so even a solver that looked would find no cell to stand in. The Porta Decumana, 2026-09-12: phase two's
/// Aetheroplasm orbs were never soaked.</para>
/// </summary>
public static class OrbIntercept
{
    /// <summary>
    /// Half-width of the path, in yalms. The usable band is this minus the safety margin either side of
    /// the path's centre line: one yalm at the default margin, which is inside the half-yalm-plus-orb reach
    /// at which an orb touches a player, so a character standing anywhere usable is run over.
    /// </summary>
    public const float PathRadius = 2f;

    /// <summary>How far ahead of an orb its path counts, in yalms, when nothing nearer stops it.</summary>
    public const float MaxLead = 8f;

    /// <summary>
    /// One zone forbidding everything except the path just ahead of each orb, or null with no orbs.
    /// Paths stop short of <paramref name="destination"/>'s hitbox when given, since an orb is never
    /// intercepted from inside the thing it is rolling towards.
    /// </summary>
    public static ShapeDistance? Zone(IReadOnlyList<Actor> orbs, Actor? destination = null)
    {
        var count = orbs.Count;
        if (count == 0)
            return null;

        var paths = new ShapeDistance[count];
        for (var i = 0; i < count; ++i)
        {
            var orb = orbs[i];
            var lead = MaxLead;
            if (destination != null)
            {
                var room = (destination.Position - orb.Position).Length() - destination.HitboxRadius - PathRadius;
                lead = Math.Clamp(room, 0.5f, MaxLead);
            }

            paths[i] = new SDInvertedCapsule(orb.Position, orb.Rotation.ToDirection(), lead, PathRadius);
        }

        return new SDIntersection(paths);
    }

    /// <summary>Contact reach of a player's hitbox, in yalms, added to an orb's own radius.</summary>
    public const float PlayerHitbox = 0.5f;

    /// <summary>
    /// The smallest radius the solver can always stand inside: its grid is a yalm apart, so no point is
    /// further than 0.71 yalms from a cell centre, and any circle at least that big contains one.
    /// </summary>
    public const float StandableRadius = 0.75f;

    /// <summary>
    /// Ground from which a player touches this orb, whether or not it is moving: a circle of the orb's own
    /// radius centred half a yalm ahead of it along its facing.
    /// <para>This replaces BossmodReborn's one-yalm box at the same centre, which seven modules copied
    /// (swept 2026-09-12). Until that day the box was never read at all, being added at MaxValue; once the
    /// solver learned to honour MaxValue positioning, its deepest point was still only half a yalm from its
    /// edge, under the 0.71 the grid is guaranteed to land inside, so the dodge could find no cell in it.</para>
    /// <para>Sized to touch, not merely to be near. The furthest point of the circle is half a yalm plus the
    /// orb's radius from the orb, which is exactly where a player's hitbox meets it; standing anywhere inside
    /// is contact. Stationary orbs are touched where they sit, and a moving one rolls into a player standing
    /// ahead of it. Where an orb is known to travel along its facing, <see cref="Zone"/> intercepts it
    /// further out instead.</para>
    /// </summary>
    public static ShapeDistance Touch(Actor orb)
        => new SDInvertedCircle(orb.Position + (PlayerHitbox * orb.Rotation.ToDirection()), MathF.Max(orb.HitboxRadius, StandableRadius));
}
