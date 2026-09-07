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
        public const int PriorityUndesirable = -1; // attackable, but something else should be hit first
        public const int PriorityPointless = -1;           // no reason to attack

        public readonly Actor Actor = actor;
        public int Priority = priority;
        public bool ShouldBeTanked = shouldBeTanked;
        public bool ShouldBeInterrupted;
        public bool ShouldBeStunned;
        public bool ShouldBeDispelled;

        // Tank-AI intent, carried for source compatibility with ported modules. Minerva does not drive a
        // tank — it dodges for whoever is playing — so nothing reads these; a module setting DesiredPosition
        // is saying where this add ought to be dragged, which is real information a future tank mode wants.
        public float AttackStrength = 0.05f;
        public WPos DesiredPosition = actor.Position;
        public Angle DesiredRotation = actor.Rotation;
        public float TankDistance = 2f;
        public bool PreferProvoking;
        public bool ForbidDOTs;
        public bool StayAtLongRange;

        /// <summary>Autoattacking this enemy hurts you back.</summary>
        public bool Spikes;

        /// <summary>Setting this true also lifts <see cref="Priority"/> out of negative territory.</summary>
        public bool ShouldBeTargeted
        {
            get;
            set
            {
                field = value;
                if (value)
                    this.Priority = Math.Max(0, this.Priority);
            }
        }
    }

    public enum SpecialMode { Normal, Pyretic, Freezing, Misdirection, NoMovement }

    public enum PredictedDamageType { None, Raidwide, Tankbuster, Shared }

    public WPos PlayerPosition;
    public WPos Center;
    public ArenaBounds Bounds = new ArenaBoundsCircle(20f);

    /// <summary>
    /// Bounds the pathfinder searches within, when a mechanic needs it to differ from the arena's own.
    /// A fight that wants you pressed against the wall sets a slightly larger box here so the search can
    /// reach spots the real bounds would round away from. Defaults to <see cref="Bounds"/>.
    /// </summary>
    public ArenaBounds? PathfindMapBounds;

    public readonly List<ForbiddenZone> ForbiddenZones = [];
    public readonly List<Enemy> PotentialTargets = [];
    public List<ShapeDistance> TemporaryObstacles = [];
    public readonly List<Func<WPos, float>> GoalZones = [];
    public readonly List<(Angle center, Angle halfWidth, DateTime activation)> ForbiddenDirections = [];
    public readonly List<(BitMask players, DateTime activation, PredictedDamageType type)> PredictedDamage = [];

    /// <summary>
    /// Instance ID of the tank currently holding a mechanic that wants passing on, or 0 when no swap is
    /// called for. Whoever this is should drop aggro; any other tank should provoke.
    ///
    /// <para>BossmodReborn states this as text — "Provoke!" or "Pass aggro!" printed at whichever tank is
    /// reading. That is enough for a person playing one character and useless for a party being driven,
    /// where the plugin has to decide which of two characters does what. The identity is the decision;
    /// the words are a rendering of it.</para>
    /// </summary>
    public ulong TankSwapCurrentTank;

    /// <summary>When the swap has to be done by — the activation of the hit that will otherwise land on
    /// the same tank twice. <c>default</c> when unknown.</summary>
    public DateTime TankSwapBy;
    public readonly List<(SpecialMode mode, DateTime activation, DateTime finish)> SpecialModes = [];
    public readonly ActionQueue ActionsToExecute = new();

    // Intent a ported module expresses that is not about where to stand. Minerva neither presses buttons
    // nor moves the player anywhere it was not asked to, so nothing reads these — but a module setting
    // InteractWithOID is recording that this fight has an object you must click, which is a real fact about
    // the fight and worth carrying rather than deleting from every port.

    /// <summary>An interactable this fight requires (a lever, an aetheryte), by OID.</summary>
    public uint InteractWithOID;

    /// <summary>Which actor to interact with, once one matching <see cref="InteractWithOID"/> is found.</summary>
    public Actor? InteractWithTarget;

    /// <summary>Portals/teleporters in the arena: where each one is and where it puts you.</summary>
    /// <summary>
    /// Ways across the arena that the floor does not connect — the wolf platforms in M08S and their kin.
    /// <para>Collected by modules, not yet routed through: the solver works over a grid of cells and has
    /// no notion of an edge that skips space. See <see cref="Pathfinding.Teleporter"/>.</para>
    /// </summary>
    public readonly List<Pathfinding.Teleporter> Teleporters = [];

    /// <summary>Force the player's target, for fights that demand a specific one.</summary>
    public Actor? ForcedTarget;

    /// <summary>Force movement in a direction regardless of the pathfinder.</summary>
    public WDir ForcedMovement;

    /// <summary>Drag the forced target to this position (tank AI).</summary>
    /// <summary>Where a module would like the boss dragged to. Recorded; the method of the same
    /// name in BossmodReborn is the one modules actually call, so this is renamed out of its way.</summary>
    public WPos? DesiredTargetLocation;

    /// <summary>Statuses the player should cancel off themselves.</summary>
    /// <summary>Statuses the fight wants cancelled, with the source that applied each. The source matters:
    /// the same status id from a different caster is not the same problem.</summary>
    public readonly List<(uint StatusId, ulong SourceId)> StatusesToCancel = [];

    /// <summary>A cleansable debuff is out and someone should Esuna it.</summary>
    public BitMask ShouldCleanse;

    /// <summary>The mechanic wants a jump (knockback-immunity windows, some platform fights).</summary>
    public bool WantJump;

    /// <summary>The fight is starting and the player is still mounted.</summary>
    public bool WantDismount;

    public void Clear()
    {
        this.ForbiddenZones.Clear();
        this.PotentialTargets.Clear();
        this.TemporaryObstacles = [];
        this.GoalZones.Clear();
        this.ForbiddenDirections.Clear();
        this.PredictedDamage.Clear();
        this.TankSwapCurrentTank = 0uL;
        this.TankSwapBy = default;
        this.SpecialModes.Clear();
        this.ActionsToExecute.Clear();
        this.PathfindMapBounds = null;
        this.Teleporters.Clear();
        this.StatusesToCancel.Clear();
        this.InteractWithOID = 0;
        this.InteractWithTarget = null;
        this.ForcedTarget = null;
        this.ForcedMovement = default;
        this.DesiredTargetLocation = null;
        this.ShouldCleanse.Reset();
        this.WantJump = false;
        this.WantDismount = false;
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

    /// <summary>BossmodReborn's name for <see cref="PotentialTargets"/>.</summary>
    public List<Enemy> Enemies => this.PotentialTargets;

    /// <summary>
    /// A goal zone that pulls the party toward wherever <paramref name="target"/> has to be dragged.
    ///
    /// <para>Returns a weight function for <see cref="GoalZones"/> rather than a position: the dodge is
    /// still free to refuse it, which is the point — "drag the boss over there" is a preference that must
    /// lose to "do not stand in the fire". Yields nothing when the target is already close enough, or when
    /// the module never declared it a target at all.</para>
    /// </summary>
    public Func<WPos, float> PullTargetToLocation(Actor target, WPos destination, float destRadius = 2f)
    {
        var enemy = this.FindEnemy(target);
        if (enemy == null)
            return static _ => 0f;

        var adjRange = enemy.TankDistance + target.HitboxRadius + 0.5f;
        var desiredToTarget = target.Position - destination;
        if (desiredToTarget.LengthSq() <= destRadius * destRadius)
            return static _ => 0f;

        var dest = destination - adjRange * desiredToTarget.Normalized();
        return GoalSingleTarget(dest, 1f, 10f);
    }

    public void AddPredictedDamage(BitMask players, DateTime activation, PredictedDamageType type = PredictedDamageType.Raidwide)
        => this.PredictedDamage.Add((players, activation, type));

    public static Func<WPos, float> GoalSingleTarget(WPos target, float radius, float weight = 1f)
        => p => p.InCircle(target, radius) ? weight : 0f;
    public static Func<WPos, float> GoalSingleTarget(Actor target, float range, float weight = 1f)
        => GoalSingleTarget(target.Position, range + target.HitboxRadius, weight);

    /// <summary>Full weight anywhere inside the ring — "stand in the donut, anywhere".</summary>
    public static Func<WPos, float> GoalDonut(WPos center, float innerRadius, float outerRadius, float weight = 1f)
        => p => p.InDonut(center, innerRadius, outerRadius) ? weight : 0f;

    /// <summary>Full weight anywhere inside the rectangle.</summary>
    public static Func<WPos, float> GoalRectangle(WPos center, WDir direction, float halfWidth, float halfHeight, float weight = 1f)
        => p => p.InRect(center, direction, halfHeight, halfHeight, halfWidth) ? weight : 0f;

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
    /// <summary>
    /// How far past a gaze's edge a turn aims. The game decides whether you are looking at an eye on its
    /// own clock, the rotation turns the character back toward its target on every action, and the turn
    /// itself interpolates over several frames -- so a heading that is only just outside the arc is not
    /// outside it by the time it matters. Wide enough to survive all three, narrow enough that a heading
    /// with room to spare is never spun further.
    /// </summary>
    public static readonly Angle GazeFacingMargin = 25f.Degrees();

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
        // NOT "already outside the arc, so we are done". A heading one degree clear of a gaze is one twitch
        // from being inside it, and the twitch always comes: the rotation auto-faces the target on every
        // action and the dodge walks the character while the eye moves. Phantom Hydra, 2026-09-06 -- a
        // five-second gaze cast, and Minerva did not begin turning until 0.7s before the snapshot, because
        // until then the facing was technically clear. It reached 35 degrees of the 70 it wanted and the
        // gaze landed. So a clear-but-marginal facing is improved during the cast, while the scoring below
        // still leaves a facing with real room alone (clearance is capped at the margin, then the shortest
        // turn wins, and no turn at all is the shortest).

        // Candidates are the arc edges pushed out by a real margin, plus the heading directly opposite
        // each arc, which is where the clearance is greatest when there is only one eye. Coverage only
        // changes at an edge, so a fewest-hit heading is always at one of these or where we already point.
        //
        // The margin is the whole point, and a death taught it. Eye to Eye, 2026-09-06: the character was
        // looking straight into a 45-degree arc and the chosen heading was a 46-degree turn -- one degree
        // clear of Minerva's own model of the arc. Against the rotation auto-facing the boss back, the
        // interpolation taking a few frames, and the server snapshotting on its own clock, one degree is
        // nothing, and the user was petrified on almost every gaze cycle with the eye glyph showing red
        // the whole time. Turning away means turning CLEAR, as BossmodReborn does by spinning right around.
        var bestHits = gazesHit;
        var bestTurn = 0f;      // the incumbent is the current facing, which is no turn at all
        var bestClear = Clearance(preferred, active);
        var best = preferred;   // a local, because an out parameter cannot be touched from a local function
        foreach (var (center, halfWidth) in active)
        {
            for (var side = -1; side <= 1; side += 2)
                Consider(new Angle(center.Rad + (side * (halfWidth.Rad + GazeFacingMargin.Rad))));
            Consider(center + 180f.Degrees());
        }

        facing = best;
        gazesHit = bestHits;
        return true;

        void Consider(Angle candidate)
        {
            var hits = Hits(candidate, active);
            var turn = MathF.Abs((candidate - preferred).Normalized().Rad);
            var clear = Clearance(candidate, active);

            // Fewer gazes always wins. Among equals, clearance up to the margin: a heading a hair outside
            // an arc is worth no more than the one inside it once anything nudges the character. Only when
            // both are clear enough does the shorter turn decide, so a safe facing is never spun for gain
            // it does not need.
            var cappedClear = MathF.Min(clear, GazeFacingMargin.Rad);
            var cappedBest = MathF.Min(bestClear, GazeFacingMargin.Rad);
            var better = hits < bestHits
                || (hits == bestHits && cappedClear > cappedBest + 0.001f)
                || (hits == bestHits && MathF.Abs(cappedClear - cappedBest) <= 0.001f && turn < bestTurn);
            if (!better)
                return;
            best = candidate;
            bestHits = hits;
            bestTurn = turn;
            bestClear = clear;
        }

        static int Hits(Angle a, List<(Angle Center, Angle HalfWidth)> arcs)
        {
            var n = 0;
            foreach (var (center, halfWidth) in arcs)
                if (MathF.Abs((a - center).Normalized().Rad) <= halfWidth.Rad)
                    ++n;
            return n;
        }

        // How far this heading is from the nearest arc it is NOT inside; negative while inside one.
        static float Clearance(Angle a, List<(Angle Center, Angle HalfWidth)> arcs)
        {
            var worst = float.MaxValue;
            foreach (var (center, halfWidth) in arcs)
                worst = MathF.Min(worst, MathF.Abs((a - center).Normalized().Rad) - halfWidth.Rad);
            return worst == float.MaxValue ? MathF.PI : worst;
        }
    }

    /// <summary>
    /// Is it safe to BE at <paramref name="to"/> right now — inside the arena, and clear of every forbidden
    /// zone and standing obstacle?
    ///
    /// <para>Deliberately not time-aware, matching BossmodReborn's <c>IsPositionSafe</c>: it answers about
    /// the zones that exist at this moment, not about what is coming. A consumer that needs "safe for the
    /// next N seconds" wants <see cref="MaxCastTime"/> instead — that is the question a hard-cast raise is
    /// really asking, and the two are not interchangeable.</para>
    /// </summary>
    public bool IsPositionSafe(WPos to)
    {
        if (this.Bounds != null && !this.Bounds.Contains(this.Center, to))
            return false;
        if (this.InObstacle(to))
            return false;
        foreach (var z in this.ForbiddenZones)
            if (z.Contains(to))
                return false;
        return true;
    }

    /// <summary>
    /// Is a dash from <paramref name="from"/> to <paramref name="to"/> safe?
    ///
    /// <para>The destination has to be safe, and on an irregularly-shaped arena the straight line must not
    /// leave the floor on the way — a dash across the mouth of a horseshoe ends somewhere legal having
    /// crossed somewhere that is not. Only checked for custom bounds, where that is possible; a circle or
    /// a square cannot be left and re-entered in a straight line.</para>
    /// </summary>
    public bool IsDashSafe(WPos from, WPos to)
    {
        if (!this.IsPositionSafe(to))
            return false;

        if (from != to && this.Bounds is ArenaBoundsCustom)
        {
            var delta = to - from;
            var len = delta.Length();
            if (len > 0f)
            {
                var wall = this.Bounds.IntersectRay(this.Center, from, delta / len);
                if (wall >= 0f && wall < len)
                    return false;
            }
        }
        return true;
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
    /// <summary>
    /// Seconds until this spot becomes lethal, or <see cref="float.MaxValue"/> when nothing there is
    /// coming. Zero when it already is. This is the question a route has to ask that a yes/no danger test
    /// cannot answer: crossing a telegraph that fires in five seconds is free, crossing the one that fires
    /// in half a second is a death, and both look identical to <see cref="InImminentDanger"/>.
    /// </summary>
    public float SecondsUntilDangerAt(WPos p, DateTime now, float margin)
    {
        if (this.InObstacle(p))
            return 0f;
        var soonest = float.MaxValue;
        foreach (var z in this.ForbiddenZones)
        {
            if (!Touches(z, p, margin))
                continue;
            var seconds = z.Activation == default ? 0f : (float)(z.Activation - now).TotalSeconds;
            soonest = MathF.Min(soonest, MathF.Max(seconds, 0f));
        }

        return soonest;

        static bool Touches(in ForbiddenZone z, WPos at, float m)
        {
            if (z.ShapeDistance is not SDShapeCheck)
                return z.ShapeDistance.Distance(at) <= m;
            if (z.Contains(at))
                return true;
            if (m <= 0f)
                return false;
            for (var i = 0; i < 8; ++i)
                if (z.Contains(at + (new Angle(i * (Angle.TwoPI / 8f)).ToDirection() * m)))
                    return true;
            return false;
        }
    }

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

        // BossmodReborn's manual tiers, for modules that say "press this NOW". They sit above the
        // general-purpose band on purpose: a module reaching for ManualEmergency is describing a fight
        // where the action is the only correct thing to do -- the duty action on Fordola, an immunity
        // before a knockback -- and a rotation weighing it against its own dps priorities would get it
        // wrong. Minerva records the queue rather than pressing anything, so these are advice.
        public const float ManualOGCD = 4001f, ManualGCD = 4999f, ManualEmergency = 9000f;
    }

    public readonly List<(ActionID action, Actor? target, float priority, float castTime, Vector3 targetPos, Angle? facingAngle)> Entries = [];

    /// <summary>
    /// Record an action the fight wants pressed. <paramref name="targetPos"/> is for ground-targeted
    /// actions — a duty's magitek cannon aimed at a spot rather than at an actor.
    /// <para>BossmodReborn's Push carries <c>expire</c> and <c>delay</c> between priority and castTime, so
    /// a 4th POSITIONAL argument means something different there than here. Every current call site names
    /// its arguments, which is what makes the divergence harmless; keep it that way.</para>
    /// </summary>
    public void Push(ActionID action, Actor? target, float priority, float castTime = default, Vector3 targetPos = default, Angle? facingAngle = null)
        => this.Entries.Add((action, target, priority, castTime, targetPos, facingAngle));

    public void Clear() => this.Entries.Clear();
}
