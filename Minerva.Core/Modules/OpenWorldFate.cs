namespace Minerva;

/// <summary>
/// A boss standing in the open world rather than in a room — Occult Crescent's FATEs, hunts, and the like.
/// <para>These have no arena. BossmodReborn models that by following the boss with a fixed 30 yalm circle,
/// and this does the same: there is no floor plan to look up, and a circle around the thing everyone is
/// standing near is a better guess than a rectangle from a recording, which only ever describes where that
/// particular pull happened to spread out.</para>
/// <para>The centre tracks the boss because open-world bosses walk. A fixed centre taken at pull time
/// drifts away from the fight, and every mechanic drawn against it drifts with it.</para>
/// <para>It exists mostly so BossmodReborn's FATE modules port unchanged: 36 of the 54 Occult Crescent
/// modules derive from it, and without it each one needs the same edit by hand.</para>
/// </summary>
public abstract class OpenWorldFate(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, primary.Position, new ArenaBoundsCircle(DefaultRadius))
{
    /// <summary>BossmodReborn's own figure for an open-world fight.</summary>
    public const float DefaultRadius = 30f;

    /// <summary>The last FATE the game reported with this boss inside it.</summary>
    private FateState fought;

    protected override void OnUpdate()
    {
        // Prefer the FATE's own boundary. It is not a sealed arena — you can walk out, and walking out
        // drops participation — so the bound is not there to say what is possible but to say what is
        // worth doing. BossmodReborn does not bound its open-world modules at all, which is why its AI
        // will occasionally dodge a mechanic by leaving the FATE entirely. A radius the game states beats
        // both that and a guess: too small refuses safe ground, too large walks you out of the fight.
        var fate = this.World.ActiveFate;

        // The game reports the FATE the player stands in, so it goes blank the moment the player steps out
        // of it — which is exactly when the bound matters, because the way back in is measured against it.
        // Iambe, 2026-09-25: the recorder was outside the FATE from 83s to 100s, across a forced march, and
        // for those seventeen seconds the bound was a circle around the boss. So keep the last one the boss
        // was inside; it is still where the fight is.
        if (fate.Active && fate.Contains(this.PrimaryActor.Position))
            this.fought = fate;
        else if (!fate.Active)
            fate = this.fought;

        if (fate.Active)
        {
            this.Center = fate.Center;
            if (this.Bounds is not ArenaBoundsCircle c || MathF.Abs(c.Radius - fate.Radius) > 0.5f)
                this.Bounds = new ArenaBoundsCircle(fate.Radius);
            return;
        }

        // no FATE reported (a hunt, a lone NM, or the table not yet populated): follow the boss, because
        // an open-world boss walks and a centre fixed at pull time drifts away from the fight
        if (!this.PrimaryActor.IsDeadOrDestroyed)
            this.Center = this.PrimaryActor.Position;
    }
}

/// <summary>
/// A boss with no authored arena at all. Same treatment as <see cref="OpenWorldFate"/>; kept as a distinct
/// name so BossmodReborn modules naming it port unchanged.
/// </summary>
public abstract class SimpleBossModule(WorldState ws, Actor primary) : OpenWorldFate(ws, primary);
