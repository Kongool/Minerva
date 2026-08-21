namespace Minerva;

/// <summary>
/// Which sides of a target are acceptable to stand on. A set, not a choice.
/// <para>BossmodReborn models this as one positional plus a held key to temporarily allow a second, which
/// works when there is a hand on the keyboard for the character being positioned. Boxing has no spare hand,
/// so the acceptable set has to be stated up front — and it usually is a set: most melee are happy with rear
/// <i>or</i> flank, and only care that they are not standing in front.</para>
/// </summary>
[Flags]
public enum Positional
{
    /// <summary>No preference — every side satisfies.</summary>
    Any = 0,
    Front = 1,
    Flank = 2,
    Rear = 4,
}

/// <summary>
/// What the dodge aims for when it is not busy avoiding something: stay within <see cref="Range"/> of the
/// target, and prefer the requested side of it.
/// <para>Bundled rather than passed loose because these four always travel together, and a solver taking
/// four bare positional arguments invites the caller to transpose two of them.</para>
/// </summary>
public readonly record struct UptimeGoal(WPos Target, Angle Rotation, float Range, Positional Positional = Positional.Any, float BoundaryMarginDeg = 15f)
{
    // the two lines that separate the three sides, measured off the target's facing
    private const float FrontFlankEdge = 45f;
    private const float FlankRearEdge = 135f;

    /// <summary>Melee reach, matching BossmodReborn's own figure. Measured from the target's hitbox.</summary>
    public const float MeleeReach = 2.6f;

    /// <summary>
    /// How close everyone else wants to be. Not an ability range — a caster reaches 25 yalms — but the
    /// distance at which they are still inside stack markers, heals and most mechanics without being in
    /// the cleave. Also measured from the hitbox, as the game measures everything.
    /// </summary>
    public const float RangedReach = 15f;

    /// <summary>What this role wants between itself and the target's hitbox.</summary>
    public static float ReachFor(Role role) => role is Role.Tank or Role.Melee ? MeleeReach : RangedReach;

    /// <summary>
    /// The goal for keeping a given player useful against a given target.
    /// <para>Roles do not share a definition of uptime, and treating them as if they did is what drags a
    /// Black Mage into a boss's melee band to shave a yard off a dodge. An unknown role is treated as
    /// ranged: standing too far back costs damage, standing too close costs the pull.</para>
    /// </summary>
    public static UptimeGoal For(Actor target, Role role, Positional positional = Positional.Any, float boundaryMarginDeg = 15f)
        => new(target.Position, target.Rotation, target.HitboxRadius + ReachFor(role), positional, boundaryMarginDeg);

    /// <summary>
    /// How far outside the useful band this point is, in yalms. Zero anywhere inside it.
    /// <para>The distinction matters more than it looks. Scoring raw distance to the target makes the goal
    /// a point attractor — every cell nearer the boss scores better, forever — which is approximately right
    /// for melee and plainly wrong for everyone else. What both roles actually want is a band: inside it,
    /// uptime is satisfied and the dodge should be spending its budget on safety and travel instead.</para>
    /// </summary>
    public float ExcessRange(WPos p)
    {
        var d = (p - this.Target).Length() - this.Range;
        return d > 0f ? d : 0f;
    }

    /// <summary>
    /// How far this point sits, in degrees, from the best place to stand inside the required arc — or null
    /// when no side is required and anywhere will do.
    /// <para>The naive answer is the centre of the arc, and that is what BossmodReborn does: asked for rear
    /// it parks you at dead rear, asked for left flank it parks you at dead left. From dead rear the flank
    /// is 45 degrees of travel away. Jobs differ in whether that matters — Samurai has a full GCD between
    /// its flank and rear hits and can afford the trip, Monk has only an oGCD and cannot.</para>
    /// <para>So the target is a margin <i>inside</i> the arc, measured from its nearest border: at 15 degrees
    /// into the rear you are unambiguously rear, and the flank is a 15 degree turn instead of 45. Both
    /// mirror sides qualify — the sign of the bearing is dropped — so whichever side of the boss you are
    /// already on is the one you get.</para>
    /// </summary>
    public float? DegreesFromPreferredStand(WPos p)
    {
        if (this.Positional == Positional.Any)
            return null;

        var off = p - this.Target;
        if (off.LengthSq() < 0.0001f)
            return null;

        var accepted = this.Positional; // copied out: a struct's local function cannot touch `this`
        var margin = MathF.Max(this.BoundaryMarginDeg, 0f);
        var rel = MathF.Abs((Angle.FromDirection(off) - this.Rotation).Normalized().Deg);
        float? best = null;

        // each arc's preferred bearing sits `margin` inside it, from the border it is most likely to be
        // asked to cross: flank prefers its rear edge, because flank/rear is the pairing melee actually use
        Consider(Positional.Front, FrontFlankEdge - margin);
        Consider(Positional.Flank, FlankRearEdge - margin);
        Consider(Positional.Rear, FlankRearEdge + margin);
        return best;

        void Consider(Positional side, float idealRel)
        {
            if ((accepted & side) == 0)
                return;
            var d = MathF.Abs(rel - idealRel);
            best = best is { } b ? MathF.Min(b, d) : d;
        }
    }

    /// <summary>Is this point on the requested side of the target?</summary>
    public bool Satisfied(WPos p)
    {
        if (this.Positional == Positional.Any)
            return true;

        var off = p - this.Target;
        if (off.LengthSq() < 0.0001f)
            return false; // standing exactly on it has no side

        var rel = MathF.Abs((Angle.FromDirection(off) - this.Rotation).Normalized().Deg);
        var side = rel switch
        {
            <= 45f => Positional.Front,
            >= 135f => Positional.Rear,
            _ => Positional.Flank,
        };

        return (this.Positional & side) != 0;
    }
}

/// <summary>Result of a dodge solve.</summary>
/// <param name="Target">Where the dodge ends up — what the guidance marker draws and what the commitment
/// is held against.</param>
/// <param name="Waypoint">Where to steer <i>right now</i>. Equal to <see cref="Target"/> on open ground,
/// and a point short of it when the route bends around something. Steering straight at the destination is
/// what walks a character into the boulder it was told to hide behind.</param>
public readonly record struct SafeSpot(bool NeedToMove, bool Found, WPos Target, WDir Direction, WPos Waypoint = default)
{
    public static readonly SafeSpot Stay = new(false, true, default, default);

    /// <summary>The point to move toward, falling back to the destination when no route was computed.</summary>
    public WPos Steer => this.Waypoint == default ? this.Target : this.Waypoint;
}

/// <summary>
/// The auto-dodge solver. If the player is (or is about to be) inside a danger zone, it rasterizes
/// the arena into a grid, discards every cell that is out of bounds or inside an imminent forbidden
/// zone, and returns the nearest surviving cell as the dodge target. Deliberately simple and
/// deterministic — nearest-safe rather than full navmesh — which is enough for telegraphed AOEs and
/// keeps it fully testable with no game.
/// </summary>
public static class ArenaPathfinder
{
    /// <summary>Unsprinted run speed, yalms per second — what reachability is judged against.</summary>
    public const float DefaultMoveSpeed = 6f;

    public static SafeSpot Solve(AIHints hints, DateTime now, float horizonSeconds = 3f, float cellSize = 1f, float safetyMargin = 0f, UptimeGoal? goal = null, float moveSpeed = DefaultMoveSpeed)
    {
        var deadline = now.AddSeconds(horizonSeconds);
        var player = hints.PlayerPosition;

        // Keep moving until we're clear by `safetyMargin` — NOT merely past the zone boundary. This test
        // has to use the same bar as the cell search below: testing "am I in danger?" without the margin
        // while requiring the margin of the destination makes the solve stop the instant the player's
        // centre crosses the edge, stranding them on the AOE rim where hitbox radius and server latency
        // still clip them. (Reported in-game as "starts to avoid but doesn't fully leave the AOE".)
        if (!hints.InImminentDanger(player, deadline, safetyMargin))
            return Regain(hints, deadline, player, cellSize, safetyMargin, goal);

        // Prefer somewhere we can actually reach before the cast lands. If nothing qualifies, widen rather
        // than freeze: partially clearing a zone beats standing in it because the ideal spot was too far.
        var budget = TimeUntilDanger(hints, player, deadline, now);
        if (TryNearestSafe(hints, deadline, player, cellSize, safetyMargin, goal, moveSpeed, budget, out var spot))
            return Settle(hints, deadline, player, spot, cellSize);
        if (TryNearestSafe(hints, deadline, player, cellSize, safetyMargin, goal, moveSpeed, float.MaxValue, out spot))
            return Settle(hints, deadline, player, spot, cellSize);
        if (safetyMargin > 0f && TryNearestSafe(hints, deadline, player, cellSize, 0f, goal, moveSpeed, float.MaxValue, out spot))
            return Settle(hints, deadline, player, spot, cellSize);

        return new SafeSpot(true, false, player, default); // whole reachable arena is dangerous
    }

    /// <summary>
    /// How long before the ground we are standing on turns lethal. A destination has to be reachable inside
    /// that, or the solver is picking somewhere the character cannot get to in time.
    /// </summary>
    private static float TimeUntilDanger(AIHints hints, WPos player, DateTime deadline, DateTime now)
    {
        var soonest = float.MaxValue;
        foreach (var z in hints.ForbiddenZones)
        {
            if (z.Activation > deadline || !z.Contains(player))
                continue;
            soonest = MathF.Min(soonest, MathF.Max((float)(z.Activation - now).TotalSeconds, 0f));
        }

        return soonest;
    }

    /// <summary>
    /// Nothing is about to hit us — so close the distance back to what we were fighting.
    /// <para>Dodging is only half of positioning. Solving purely for safety leaves the character standing
    /// wherever the last AOE pushed them, out of range, until the next one arrives: the danger has passed
    /// but the reason to be near the boss has not. BossmodReborn gets the walk back for free because its
    /// goal zones are added every frame and merely *overridden* by danger, so when the danger lifts the pull
    /// is still there.</para>
    /// <para>Stays put when already in position, when there is nothing to return to, or when the way back is
    /// itself unsafe — walking into the next AOE to regain uptime is not a trade worth making.</para>
    /// </summary>
    private static SafeSpot Regain(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin, UptimeGoal? goal)
    {
        if (goal is not { } g || g.Range <= 0f)
            return SafeSpot.Stay;
        if ((player - g.Target).Length() <= g.Range && g.Satisfied(player))
            return SafeSpot.Stay;

        // Two passes rather than a penalty. Returning to the boss is a walk we are making anyway, and the
        // extra distance to arrive on the correct side is at most the width of melee range — so insist on
        // the positional first, and only settle for the wrong side if the right one is unreachable or
        // unsafe. A weighted tiebreak cannot express that: from far enough out the nearest cell always wins
        // however large the weight, and a weight big enough to overcome it would drag the dodge around the
        // arena in the cases where distance genuinely does matter.
        if (Nearest(hints, deadline, player, cellSize, margin, g, requirePositional: true) is { } onSide)
            return onSide;
        return Nearest(hints, deadline, player, cellSize, margin, g, requirePositional: false) ?? SafeSpot.Stay;
    }

    private static SafeSpot? Nearest(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin, UptimeGoal g, bool requirePositional)
    {
        var center = hints.Center;
        var reach = hints.Bounds.Radius;
        var best = player;
        var bestCost = float.MaxValue;
        var found = false;

        for (var x = center.X - reach; x <= center.X + reach; x += cellSize)
        {
            for (var z = center.Z - reach; z <= center.Z + reach; z += cellSize)
            {
                var p = new WPos(x, z);
                if ((p - g.Target).Length() > g.Range)
                    continue;                                  // only cells that actually restore range
                if (requirePositional && !g.Satisfied(p))
                    continue;
                if (!hints.Bounds.Contains(center, p))
                    continue;
                if (hints.InImminentDanger(p, deadline, margin))
                    continue;

                var cost = (p - player).LengthSq();             // nearest such cell: the shortest walk back
                cost -= AnchorBonus(g, p);                      // ... favouring a spot we can switch side from
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = p;
                    found = true;
                }
            }
        }

        if (!found)
            return null;
        return (best - player).LengthSq() > cellSize * cellSize
            ? new SafeSpot(true, true, best, (best - player).Normalized())
            : SafeSpot.Stay;
    }

    /// <summary>
    /// When the best cell we can reach is the one we're already standing on, there is nothing to gain by
    /// moving: hold, provided we aren't actually inside a zone. This is what keeps the margin-based
    /// "need to move" test from twitching forever in arenas too constrained to satisfy the margin — e.g. a
    /// donut whose safe centre is smaller than the margin itself.
    /// </summary>
    private static SafeSpot Settle(AIHints hints, DateTime deadline, WPos player, SafeSpot spot, float cellSize)
        => (spot.Target - player).LengthSq() <= cellSize * cellSize && !hints.InImminentDanger(player, deadline)
            ? SafeSpot.Stay
            : spot;

    /// <summary>
    /// How much further the dodge will walk to land inside a goal zone, in yalms, at full goal weight.
    /// <para>Expressed as distance rather than as a flat score because the score is a <i>square</i> of
    /// distance: a fixed 9-point bonus is worth two yalms at the origin and half a yalm at eight, which is
    /// to say it decided nothing at the ranges dodges actually happen. Stated in yalms it means the same
    /// thing everywhere — "a goal is worth this much extra walking" — and cannot quietly stop applying as
    /// the fight spreads out.</para>
    /// <para>It still never overrides safety: an unsafe cell is discarded before it is ever scored.</para>
    /// </summary>
    private const float GoalWorthYalms = 2f;

    /// <summary>
    /// How much a yard <i>outside the useful band</i> costs, relative to a yard of travel. Below 1 the dodge
    /// still prefers the nearest safe cell and only breaks ties toward the band; at 1 it will accept a
    /// noticeably longer run to stay in it. Kept under 1 so safety and travel still lead.
    /// </summary>
    private const float UptimeWeight = 0.6f;

    /// <summary>
    /// What the wrong side of the target costs, in yards² of extra walking. Enough to break a tie between
    /// otherwise equal cells, small enough that it never sends the character across the arena chasing a
    /// flank — a positional is worth a step, never a death.
    /// </summary>
    private const float PositionalPenalty = 16f;

    /// <summary>
    /// What standing where the next positional is cheap to reach is worth, in yards² of walking. Enough to
    /// choose between otherwise equal cells in the arc, never enough to drag the character off safe ground.
    /// </summary>
    private const float AnchorBias = 6f;

    /// <summary>Degrees of slop around the preferred bearing that still counts as well-placed.</summary>
    private const float AnchorToleranceDeg = 20f;

    private static float AnchorBonus(UptimeGoal g, WPos p)
    {
        if (g.DegreesFromPreferredStand(p) is not { } deg || deg > AnchorToleranceDeg)
            return 0f;
        return AnchorBias * (1f - (deg / AnchorToleranceDeg)); // strongest on the anchor, fading to nothing
    }

    private static bool TryNearestSafe(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin, UptimeGoal? goal, float moveSpeed, float timeBudget, out SafeSpot spot)
    {
        var hasGoals = hints.GoalZones.Count > 0;
        var found = false;
        var best = player;
        var bestCost = float.MaxValue;

        // Cost by route, not by displacement. Everything below scores a cell on what it takes to GET there,
        // so a safe wedge behind a rock is priced with the walk around the rock included, and an unreachable
        // one is not considered at all.
        var grid = new RouteGrid(hints, deadline, player, cellSize, margin);

        for (var gz = 0; gz < grid.Height; ++gz)
        {
            for (var gx = 0; gx < grid.Width; ++gx)
            {
                if (grid.Blocked(gx, gz) || grid.Risky(gx, gz))
                    continue;

                var p = grid.Center(gx, gz);
                if (float.IsInfinity(grid.CostAt(gx, gz)))
                    continue;                                   // nothing walks there from here

                // Scored on displacement, routed on cost. The route decides which cells exist and which
                // way to go; it deliberately does not price them, because its danger penalty is worth
                // several yalms a step and would swamp every weight below that is calibrated in yalms.
                var travel = (p - player).Length();
                if (moveSpeed > 0f && timeBudget != float.MaxValue && travel / moveSpeed > timeBudget)
                    continue;                                   // cannot get there before it lands

                // cost is travel distance, discounted for cells inside goal zones (a gentle attractor)
                if (hasGoals)
                    travel = MathF.Max(travel - (hints.GoalScore(p) * GoalWorthYalms), 0f);
                var cost = travel * travel;

                // ... and increased the further the cell sits from whatever we want uptime on, so that among
                // safe ground the dodge gives up as little melee range as it can rather than simply taking
                // the first cell outside the AOE
                if (goal is { } g)
                {
                    var excess = g.ExcessRange(p);
                    cost += excess * excess * UptimeWeight;
                    if (!g.Satisfied(p))
                        cost += PositionalPenalty;
                    cost -= AnchorBonus(g, p);
                }

                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = p;
                    found = true;
                }
            }
        }

        if (!found)
        {
            spot = default;
            return false;
        }

        var waypoint = grid.Waypoint(player, best);
        spot = new SafeSpot(true, true, best, (waypoint - player).Normalized(), waypoint);
        return true;
    }
}
