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
        public bool ShouldBeDispelled;
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
    /// <summary>
    /// The active special mode at <paramref name="now"/>, or <see cref="SpecialMode.Normal"/>. A mode counts
    /// as active once its activation has passed and until its finish (a default finish means "until cleared").
    /// </summary>
    public SpecialMode ActiveSpecialMode(DateTime now)
    {
        foreach (var (mode, activation, finish) in this.SpecialModes)
            if (activation <= now && (finish == default || finish > now))
                return mode;
        return SpecialMode.Normal;
    }

    /// <summary>
    /// True while any player action would punish — a Pyretic-style "stop everything" mechanic. Rotation
    /// plugins consume this (via the plugin's IPC) to hard-pause; movement is separately forbidden by
    /// <see cref="MustNotMove"/>.
    /// </summary>
    public bool MustNotAct(DateTime now) => this.ActiveSpecialMode(now) == SpecialMode.Pyretic;

    /// <summary>True while movement would punish (Pyretic-style, or a movement-only stand-still mechanic).</summary>
    public bool MustNotMove(DateTime now) => this.ActiveSpecialMode(now) is SpecialMode.Pyretic or SpecialMode.NoMovement;

    /// <summary>
    /// When the next window that punishes acting begins, or null if none is known.
    /// <para>The boolean above is present tense, and a rotation that only reads present tense reacts one
    /// GCD too late: it has already committed a two-and-a-half second cast that resolves inside the
    /// mechanic. Knowing the window is coming is what lets it hold instead of interrupting.</para>
    /// </summary>
    public DateTime? NextMustNotAct(DateTime now) => this.NextMode(now, static m => m == SpecialMode.Pyretic);

    /// <summary>When the next window that punishes movement begins, or null if none is known.</summary>
    public DateTime? NextMustNotMove(DateTime now) => this.NextMode(now, static m => m is SpecialMode.Pyretic or SpecialMode.NoMovement);

    private DateTime? NextMode(DateTime now, Func<SpecialMode, bool> match)
    {
        DateTime? soonest = null;
        foreach (var (mode, activation, finish) in this.SpecialModes)
        {
            if (!match(mode) || (finish != default && finish <= now))
                continue;
            var at = activation < now ? now : activation; // already inside it: it begins now
            if (soonest is not { } s || at < s)
                soonest = at;
        }

        return soonest;
    }

    /// <summary>
    /// How long the player can stand here and cast before something forces them to move — the budget a
    /// hardcast has to fit inside. <see cref="float.MaxValue"/> when nothing pending touches this spot.
    /// <para>It is not simply "when does the AOE land". Leaving costs time too, so a zone landing in six
    /// seconds that takes three to walk out of affords a three second cast, not a six second one. The walk
    /// is priced from the signed distance: inside a zone that value is how deep in you are, which is the
    /// distance back out, and the clearance margin is added because stopping on the rim is not clear.</para>
    /// <para>Matches BossmodReborn's <c>Hints.MaxCastTime</c> in meaning and units, so a consumer already
    /// asking BMR that question can ask this one the same way. BMR derives it from its pathfinder's leeway;
    /// this derives it from the geometry, which needs no solve and answers past the dodge's own horizon —
    /// a raise is eight seconds and the dodge only looks five ahead.</para>
    /// </summary>
    public float MaxCastTime(DateTime now, float margin = 0f, float moveSpeed = 6f)
    {
        var budget = float.MaxValue;

        // a stand-still punisher ends the cast outright, whatever the ground is doing
        if (this.MustNotAct(now))
            return 0f;
        if (this.NextMustNotAct(now) is { } noAct)
            budget = MathF.Max((float)(noAct - now).TotalSeconds, 0f);

        foreach (var z in this.ForbiddenZones)
        {
            var walk = EscapeDistance(z.ShapeDistance, this.PlayerPosition, margin);
            if (walk <= 0f)
                continue;                                       // this one never asks us to move

            var lands = MathF.Max((float)(z.Activation - now).TotalSeconds, 0f);
            budget = MathF.Min(budget, MathF.Max(lands - (walk / MathF.Max(moveSpeed, 0.01f)), 0f));
        }

        return budget;
    }

    /// <summary>How far out of a zone the player has to walk to be clear of it by <paramref name="margin"/>.</summary>
    /// <remarks>
    /// Every primitive shape can answer this directly. What cannot is a boolean combination complex enough
    /// to fall back to <see cref="SDShapeCheck"/>, which reports ±1 for "in" and "out" and nothing about
    /// depth; those get searched instead, rings outward until one comes up clear. That search is expensive
    /// per grid cell, which is why the pathfinder avoids it, and free here — once a frame, for one point.
    /// </remarks>
    private static float EscapeDistance(ShapeDistance z, WPos p, float margin)
    {
        if (z is not SDShapeCheck)
        {
            var d = z.Distance(p);
            return d > margin ? 0f : MathF.Max(margin - d, 0f);
        }

        if (!z.Contains(p) && margin <= 0f)
            return 0f;

        const float step = 1f;
        const float maxSearch = 30f;
        for (var r = z.Contains(p) ? step : margin; r <= maxSearch; r += step)
        {
            // the nearest way out, not a way out in every direction -- one clear bearing is an escape
            for (var i = 0; i < 8; ++i)
            {
                var a = (i * 45f).Degrees();
                if (!z.Contains(p + (a.ToDirection() * r)))
                    return r;
            }
        }

        return maxSearch; // hemmed in: charge the full search rather than pretend it is free
    }

    /// <summary>
    /// When the soonest gaze snapshots facing, or null if none is pending.
    /// <para>What a rotation needs is not "a gaze is happening" but how long it has: the game turns you
    /// toward your target when you act, so the question is whether the next ability resolves before or
    /// after the snapshot.</para>
    /// </summary>
    public DateTime? NextGazeResolve(DateTime now)
    {
        DateTime? soonest = null;
        foreach (var (_, _, activation) in this.ForbiddenDirections)
        {
            var at = activation == default || activation < now ? now : activation;
            if (soonest is not { } s || at < s)
                soonest = at;
        }

        return soonest;
    }

    public void AddPredictedDamage(BitMask players, DateTime activation, PredictedDamageType type = PredictedDamageType.Raidwide)
        => this.PredictedDamage.Add((players, activation, type));

    public static Func<WPos, float> GoalSingleTarget(WPos target, float radius, float weight = 1f)
        => p => p.InCircle(target, radius) ? weight : 0f;
    public static Func<WPos, float> GoalSingleTarget(Actor target, float range, float weight = 1f)
        => GoalSingleTarget(target.Position, range + target.HitboxRadius, weight);

    /// <summary>
    /// A graded pull toward a point: full weight on it, fading to nothing at <paramref name="maxDistance"/>.
    /// <para>Unlike <see cref="GoalSingleTarget"/>, which is a flat circle, this distinguishes "nearly there"
    /// from "just inside the edge" — so a solve that cannot reach the ideal spot still moves toward it
    /// rather than settling for any cell in the circle. Matches BossmodReborn's <c>GoalProximity</c>, which
    /// ported modules use to steer toward a specific safe spot (an isolated orb, a gap between cones).</para>
    /// </summary>
    public static Func<WPos, float> GoalProximity(WPos destination, float maxDistance, float maxWeight)
    {
        var invDistSq = 1f / MathF.Max(maxDistance * maxDistance, 1e-4f);
        return p => maxWeight * (1f - Math.Clamp(invDistSq * (p - destination).LengthSq(), 0f, 1f));
    }

    public static Func<WPos, float> GoalProximity(Actor target, float range, float weight = 1f)
        => GoalProximity(target.Position, range + target.HitboxRadius, weight);

    /// <summary>Combined attractor weight of a point across all goal zones (higher = more desirable).</summary>
    public float GoalScore(WPos p)
    {
        var score = 0f;
        for (var i = 0; i < this.GoalZones.Count; ++i)
            score += this.GoalZones[i](p);
        return score;
    }

    /// <summary>
    /// Find a facing that satisfies every gaze arc resolving by <paramref name="deadline"/>.
    /// <para>Overlapping gazes are the case that needs answering: one eye is trivial — turn around — but a
    /// boss gazing from the centre while orbs gaze from around the arena can forbid so much of the circle
    /// that the remaining gap is nowhere near where you happen to be looking, or has closed entirely.
    /// Returning false is real information: it means no facing survives and something has to give.</para>
    /// <para>Exact for a union of arcs. If a gap exists, one of its ends is the edge of some arc, so testing
    /// the edges finds it — no sampling and no resolution to tune.</para>
    /// </summary>
    public bool TryFindSafeFacing(DateTime deadline, Angle preferred, out Angle facing)
        => !this.TryFindBestFacing(deadline, preferred, out facing, out var hit) || hit == 0;

    /// <summary>
    /// The facing hit by the fewest gazes, and how many that is. Zero means genuinely safe.
    /// <para>Whether a fully safe heading exists is not the interesting question in a fight built out of
    /// gazes. Four eyes spaced evenly around a player cover every heading between them — each forbids 90
    /// degrees, and 4x90 is the whole circle — so "no safe facing" is a normal state late in such a fight,
    /// not an error. Refusing to turn at all in that state is the worst available answer: it leaves the
    /// character looking wherever it happened to be looking, which may be into all of them. One gaze
    /// instead of three is the difference worth having.</para>
    /// <para>Returns false when nothing constrains facing at all.</para>
    /// </summary>
    public bool TryFindBestFacing(DateTime deadline, Angle preferred, out Angle facing, out int gazesHit)
    {
        facing = preferred;
        gazesHit = 0;

        var active = new List<(Angle Center, Angle HalfWidth)>();
        foreach (var (center, halfWidth, activation) in this.ForbiddenDirections)
            if (activation == default || activation <= deadline)
                active.Add((center, halfWidth));

        if (active.Count == 0)
            return false;

        gazesHit = Hits(preferred, active);
        if (gazesHit == 0)
            return true;

        // Candidates are the arc edges, nudged just outside. Coverage only changes at an edge, so the
        // fewest-hit heading is always either at one or where we already point.
        const float epsilon = 0.02f; // ~1.1 degrees, comfortably inside the game's own facing tolerance
        var bestHits = gazesHit;
        var bestTurn = 0f; // the incumbent is the current facing, which is no turn at all
        foreach (var (center, halfWidth) in active)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                var candidate = new Angle(center.Rad + (side * (halfWidth.Rad + epsilon)));
                var hits = Hits(candidate, active);
                var turn = MathF.Abs((candidate - preferred).Normalized().Rad);

                // fewer gazes always wins; among equals, the shorter turn. An equal-hit candidate never
                // displaces the current facing, so the character is not spun around for no gain.
                if (hits < bestHits || (hits == bestHits && turn < bestTurn))
                {
                    facing = candidate;
                    bestHits = hits;
                    bestTurn = turn;
                }
            }
        }

        gazesHit = bestHits;
        return true;

        static int Hits(Angle a, List<(Angle Center, Angle HalfWidth)> arcs)
        {
            var n = 0;
            foreach (var (center, halfWidth) in arcs)
                if (MathF.Abs((a - center).Normalized().Rad) <= halfWidth.Rad)
                    ++n;
            return n;
        }
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
        if (this.InObstacle(p))
            return true;

        foreach (var z in this.ForbiddenZones)
        {
            if (z.Activation > deadline)
                continue;

            // A shape that can measure answers directly: "within margin" is just a signed distance below it.
            // The ring probe below exists only for shapes that cannot, and it costs nine evaluations per
            // point — on a line-of-sight zone across a full grid search that was most of the solve. Every
            // primitive now measures, so what still lands here is the complex boolean combinations only.
            if (z.ShapeDistance is not SDShapeCheck)
            {
                // <=, not <. At margin 0 this has to agree with Contains, which is "distance <= 0" -- and a
                // cell exactly on an AOE's edge is inside it, not clear of it. With < the solver was free to
                // stop dead on the rim of a circle whose radius happened to land on the grid.
                if (z.ShapeDistance.Distance(p) <= margin)
                    return true;
                continue;
            }

            if (z.Contains(p))
                return true;
            if (margin <= 0f)
                continue;
            for (var i = 0; i < 8; ++i)
            {
                var dir = new Angle(i * (Angle.TwoPI / 8f)).ToDirection();
                if (z.Contains(p + (dir * margin)))
                    return true;
            }
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
