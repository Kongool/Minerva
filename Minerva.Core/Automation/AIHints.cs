namespace Minerva;

/// <summary>A danger area the AI should keep out of, resolving at <see cref="Activation"/>. Backed by a
/// <see cref="ShapeDistance"/> so any shape (analytic or boolean) and any boolean combination works.</summary>
public readonly struct ForbiddenZone(ShapeDistance shapeDistance, DateTime activation, ulong source = default)
{
    public readonly ShapeDistance ShapeDistance = shapeDistance;
    public readonly DateTime Activation = activation;
    public readonly ulong Source = source;

    public bool Contains(WPos p) => this.ShapeDistance.Distance(p) <= 0f;
}

/// <summary>
/// Per-frame decision input for the auto-dodge engine: where the player is, the arena, and the
/// danger zones the active module's components contributed. Rebuilt each frame (see
/// <see cref="Clear"/>) and consumed by <see cref="ArenaPathfinder"/>. Game-free and testable.
/// The enemy-targeting / goal-zone / predicted-damage / special-mode members mirror BossmodReborn's
/// AIHints (BSD-3; see THIRD-PARTY-NOTICES.txt) so ported modules compile; Minerva is avoidance-only,
/// so its pathfinder consumes <see cref="ForbiddenZones"/> and <see cref="Bounds"/> — the rest are
/// recorded for modules/future use but do not yet drive movement.
/// </summary>
public sealed class AIHints
{
    /// <summary>A hostile actor a module flags as a (potential) target, with a priority.</summary>
    public sealed class Enemy(Actor actor, int priority = 0, bool shouldBeTanked = false)
    {
        public const int PriorityForbidden = int.MinValue; // must not be attacked
        public const int PriorityInvincible = int.MinValue + 1; // currently invincible
        public const int PriorityPointless = -1;           // no reason to attack

        public readonly Actor Actor = actor;
        public int Priority = priority;
        public bool ShouldBeTanked = shouldBeTanked;
        public bool ShouldBeInterrupted;
        public bool ShouldBeStunned;
    }

    public enum SpecialMode { Normal, Pyretic, Freezing, Misdirection, NoMovement }

    public enum PredictedDamageType { None, Raidwide, Tankbuster, Shared }

    public WPos PlayerPosition;
    public WPos Center;
    public ArenaBounds Bounds = new ArenaBoundsCircle(20f);

    public readonly List<ForbiddenZone> ForbiddenZones = [];
    public readonly List<Enemy> PotentialTargets = [];
    public List<ShapeDistance> TemporaryObstacles = [];
    public readonly List<Func<WPos, float>> GoalZones = [];
    public readonly List<(Angle center, Angle halfWidth, DateTime activation)> ForbiddenDirections = [];
    public readonly List<(BitMask players, DateTime activation, PredictedDamageType type)> PredictedDamage = [];
    public readonly List<(SpecialMode mode, DateTime activation, DateTime finish)> SpecialModes = [];
    public readonly ActionQueue ActionsToExecute = new();

    public void Clear()
    {
        this.ForbiddenZones.Clear();
        this.PotentialTargets.Clear();
        this.TemporaryObstacles = [];
        this.GoalZones.Clear();
        this.ForbiddenDirections.Clear();
        this.PredictedDamage.Clear();
        this.SpecialModes.Clear();
        this.ActionsToExecute.Clear();
    }

    // --- forbidden zones ---
    public void AddForbiddenZone(ShapeDistance shapeDistance, DateTime activation = default, ulong source = default)
        => this.ForbiddenZones.Add(new ForbiddenZone(shapeDistance, activation, source));

    public void AddForbiddenZone(AOEShape shape, WPos origin, Angle rotation = default, DateTime activation = default, ulong source = default)
        => this.ForbiddenZones.Add(new ForbiddenZone(shape.Distance(origin, rotation), activation, source));

    public void AddForbiddenZone(in AOEInstance aoe)
        => this.ForbiddenZones.Add(new ForbiddenZone(aoe.Shape.Distance(aoe.Origin, aoe.Rotation), aoe.Activation));

    // --- enemy targeting (recorded; Minerva does not auto-target) ---
    public Enemy? FindEnemy(Actor? actor)
    {
        if (actor == null)
            return null;
        for (var i = 0; i < this.PotentialTargets.Count; ++i)
            if (this.PotentialTargets[i].Actor == actor)
                return this.PotentialTargets[i];
        return null;
    }

    public void SetPriority(Actor? actor, int priority)
    {
        if (this.FindEnemy(actor) is { } e)
            e.Priority = priority;
    }

    public void PrioritizeTargetsByOID(uint oid, int priority)
    {
        foreach (var e in this.PotentialTargets)
            if (e.Actor.OID == oid)
                e.Priority = priority;
    }

    public void PrioritizeTargetsByOID(uint[] oids, int priority)
    {
        foreach (var e in this.PotentialTargets)
            if (Array.IndexOf(oids, e.Actor.OID) >= 0)
                e.Priority = priority;
    }

    public void PrioritizeTargetsByOIDAndForbidDOTs(uint oid, int priority, bool forbidDots) => this.PrioritizeTargetsByOID(oid, priority);

    // --- goal zones / obstacles / directions / predicted damage / special modes ---
    // TemporaryObstacles and GoalZones DO drive the auto-dodge (obstacles are avoided, goal zones bias
    // the dodge target). PredictedDamage (mitigation timing) and ForbiddenDirections (facing/gaze) and
    // SpecialModes are recorded for modules/inspection — they are outside an avoidance-only dodge's remit.
    public void AddSpecialMode(SpecialMode mode, DateTime activation, DateTime finish = default)
        => this.SpecialModes.Add((mode, activation, finish));
    public void AddPredictedDamage(BitMask players, DateTime activation, PredictedDamageType type = PredictedDamageType.Raidwide)
        => this.PredictedDamage.Add((players, activation, type));

    public static Func<WPos, float> GoalSingleTarget(WPos target, float radius, float weight = 1f)
        => p => p.InCircle(target, radius) ? weight : 0f;
    public static Func<WPos, float> GoalSingleTarget(Actor target, float range, float weight = 1f)
        => GoalSingleTarget(target.Position, range + target.HitboxRadius, weight);

    /// <summary>Combined attractor weight of a point across all goal zones (higher = more desirable).</summary>
    public float GoalScore(WPos p)
    {
        var score = 0f;
        for (var i = 0; i < this.GoalZones.Count; ++i)
            score += this.GoalZones[i](p);
        return score;
    }

    /// <summary>True if a point sits inside a standing obstacle (always dangerous, no activation time).</summary>
    public bool InObstacle(WPos p)
    {
        for (var i = 0; i < this.TemporaryObstacles.Count; ++i)
            if (this.TemporaryObstacles[i].Contains(p))
                return true;
        return false;
    }

    /// <summary>Is a point inside any zone that resolves at or before <paramref name="deadline"/> (or any standing obstacle)?</summary>
    public bool InImminentDanger(WPos p, DateTime deadline)
    {
        if (this.InObstacle(p))
            return true;
        foreach (var z in this.ForbiddenZones)
            if (z.Activation <= deadline && z.Contains(p))
                return true;
        return false;
    }

    /// <summary>
    /// Like <see cref="InImminentDanger(WPos, DateTime)"/>, but also rejects a point within <paramref name="margin"/>
    /// of a danger zone (sampled on a ring around it). Used to pick a dodge target that keeps clearance from the
    /// AOE edge — accounting for hitbox, reaction, and stopping distance — instead of landing right against it.
    /// </summary>
    public bool InImminentDanger(WPos p, DateTime deadline, float margin)
    {
        if (this.InImminentDanger(p, deadline))
            return true;
        if (margin > 0f)
            for (var i = 0; i < 8; ++i)
            {
                var dir = new Angle(i * (Angle.TwoPI / 8f)).ToDirection();
                if (this.InImminentDanger(p + dir * margin, deadline))
                    return true;
            }
        return false;
    }
}

/// <summary>
/// A minimal stand-in for BossmodReborn's action queue so ported components that push heal/utility
/// actions compile. Minerva's auto-dodge does not execute actions, so these are recorded only.
/// </summary>
public sealed class ActionQueue
{
    public static class Priority
    {
        public const float Minimal = 0f, Low = 1000f, Medium = 2000f, High = 3000f, VeryHigh = 4000f;
    }

    public readonly List<(ActionID action, Actor? target, float priority, float castTime)> Entries = [];

    public void Push(ActionID action, Actor? target, float priority, float castTime = default)
        => this.Entries.Add((action, target, priority, castTime));

    public void Clear() => this.Entries.Clear();
}
