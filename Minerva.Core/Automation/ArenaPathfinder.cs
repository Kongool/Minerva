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

    /// <summary>
    /// How much of a switch back across the border the floor may cost, in seconds of running. Half a
    /// switch: the other half is the depth on the far side. 0.8s for the round trip fits inside the
    /// shortest GCD with room for the animation lock, which is the constraint a Monk or Ninja with
    /// back-to-back positionals actually lives under.
    /// </summary>
    public const float SwitchBudgetSeconds = 0.4f;

    /// <summary>
    /// How far inside its border a side has to be before it counts as held -- the inner edge of the band
    /// whose outer edge is <see cref="BoundaryMarginDeg"/>. Past it, a step of lag or the boss turning a
    /// few degrees no longer decides the hit; the zero-yalm edge, where an AOE sometimes lands and
    /// sometimes does not, is exactly what a positional must never be judged on.
    /// <para>Set by distance, not degrees, because degrees are not what you walk: fifteen of them are a
    /// yalm and a half around a small mob and eight around a boss with a huge hitbox. The floor is the depth
    /// that costs <see cref="SwitchBudgetSeconds"/> of running at this target's stand radius -- so a small
    /// mob gets the whole band for free, a huge one keeps the floor shallow enough that the switch back
    /// still fits a GCD -- never shallower than a third of the margin, never deeper than the margin.</para>
    /// </summary>
    public float MinimumDepthDeg
    {
        get
        {
            var margin = MathF.Max(this.BoundaryMarginDeg, 0f);
            if (margin <= 0f || this.Range <= 0.01f)
                return margin;
            var budgetYalms = SwitchBudgetSeconds * ArenaPathfinder.DefaultMoveSpeed;
            var byDistance = (budgetYalms / this.Range).Radians().Deg;
            return Math.Clamp(byDistance, margin / 3f, margin);
        }
    }

    /// <summary>
    /// Is this point on the requested side AND at least <see cref="MinimumDepthDeg"/> inside it? The test
    /// a side switch has to pass: the nearest cell that merely satisfies the side sits a step past the
    /// border, where the boss turning a few degrees puts you straight back on the wrong one. Any accepted
    /// side counts, on either side of the target; the nearest such cell is what keeps the switch short.
    /// </summary>
    public bool SatisfiedInside(WPos p)
    {
        if (this.Positional == Positional.Any)
            return true;
        var off = p - this.Target;
        if (off.LengthSq() < 0.0001f)
            return false;
        var rel = MathF.Abs((Angle.FromDirection(off) - this.Rotation).Normalized().Deg);
        var m = this.MinimumDepthDeg;
        if ((this.Positional & Positional.Front) != 0 && rel <= FrontFlankEdge - m)
            return true;
        if ((this.Positional & Positional.Flank) != 0 && rel >= FrontFlankEdge + m && rel <= FlankRearEdge - m)
            return true;
        if ((this.Positional & Positional.Rear) != 0 && rel >= FlankRearEdge + m)
            return true;
        return false;
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
/// <param name="Route">The whole path to <see cref="Target"/>, for a mover that follows paths rather
/// than being steered frame by frame. Null when no grid route was computed, in which case a follower
/// should be given <see cref="Steer"/> alone.</param>
public readonly record struct SafeSpot(bool NeedToMove, bool Found, WPos Target, WDir Direction, WPos Waypoint = default, IReadOnlyList<WPos>? Route = null)
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

    public static SafeSpot Solve(AIHints hints, DateTime now, float horizonSeconds = 3f, float cellSize = 1f, float safetyMargin = 0f, UptimeGoal? goal = null, float moveSpeed = DefaultMoveSpeed, float clearanceLead = 0f)
    {
        // Solve as though it were already clearanceLead seconds later. One shift buys both halves of being
        // early: a zone becomes imminent that much sooner (the deadline moves with it), and the budget to
        // escape one shrinks by the same amount, so a cell that can only just be reached in time no longer
        // qualifies. Doing it here rather than at each comparison keeps the two consistent -- treating a
        // zone as urgent while still believing there is time to stroll out of it is how a dodge ends up
        // leaving 0.39s before impact.
        var solveNow = clearanceLead > 0f ? now.AddSeconds(clearanceLead) : now;
        var deadline = solveNow.AddSeconds(horizonSeconds);
        var player = hints.PlayerPosition;

        // Keep moving until we're clear by `safetyMargin` — NOT merely past the zone boundary. This test
        // has to use the same bar as the cell search below: testing "am I in danger?" without the margin
        // while requiring the margin of the destination makes the solve stop the instant the player's
        // centre crosses the edge, stranding them on the AOE rim where hitbox radius and server latency
        // still clip them. (Reported in-game as "starts to avoid but doesn't fully leave the AOE".)
        if (!hints.InImminentDanger(player, deadline, safetyMargin))
        {
            // Nothing is about to land -- but if the ground underfoot is going to fire at all, leaving now
            // is the cheap version of leaving later. Pallmagia, 2026-09-06: the character stood in a
            // 30-yalm circle for eight seconds because it was not yet imminent, then had to cross the arena
            // with 0.7s to spare and was caught by a cone on the way. The user's call, and the right one:
            // "i would leave earlier, it could cost the run if something lethal was inside of those aoe's".
            var uptime = Regain(hints, deadline, player, cellSize, safetyMargin, goal);
            return DriftOffDoomedGround(hints, solveNow, player, cellSize, safetyMargin, goal, moveSpeed, uptime);
        }

        // Prefer somewhere we can actually reach before the cast lands. If nothing qualifies, widen rather
        // than freeze: partially clearing a zone beats standing in it because the ideal spot was too far.
        var budget = TimeUntilDanger(hints, player, deadline, solveNow);
        if (TryNearestSafe(hints, deadline, solveNow, player, cellSize, safetyMargin, goal, moveSpeed, budget, out var spot))
            return Settle(hints, deadline, player, spot, cellSize, safetyMargin);
        if (TryNearestSafe(hints, deadline, solveNow, player, cellSize, safetyMargin, goal, moveSpeed, float.MaxValue, out spot))
            return Settle(hints, deadline, player, spot, cellSize, safetyMargin);
        if (safetyMargin > 0f && TryNearestSafe(hints, deadline, solveNow, player, cellSize, 0f, goal, moveSpeed, float.MaxValue, out spot))
            return Settle(hints, deadline, player, spot, cellSize, 0f);

        // Nothing is safe from everything inside the horizon. Before giving up, ask the same question with
        // a nearer deadline: the latest activation for which some reachable cell is clear of all that lands
        // by then. Alexander's Divine Arrow (2026-09-06): two waves of lines two seconds apart covered the
        // whole floor within the five-second horizon, and the dodge stood in the first wave for three
        // seconds reporting "no safe spot" while a cell four yalms away was clear of it. Every frame
        // re-solves, so the second wave is dodged from wherever the first was dodged to, as a person does.
        foreach (var earlier in ActivationsBefore(hints, deadline, solveNow))
            if (TryNearestSafe(hints, earlier, solveNow, player, cellSize, safetyMargin, goal, moveSpeed, float.MaxValue, out spot))
                return Settle(hints, earlier, player, spot, cellSize, safetyMargin);

        return new SafeSpot(true, false, player, default); // whole reachable arena is dangerous
    }

    /// <summary>How far ahead ground counts as "going to fire". Past this it is scenery -- a persistent
    /// voidzone publishes an activation of DateTime.MaxValue, and drifting away from those forever is not
    /// the point.</summary>
    private const float DriftLookAhead = 30f;

    /// <summary>
    /// How far clear ground has to be before leaving early is worth it. Under this, stepping out at the
    /// last moment is fine and the horizon can be trusted -- which is what every other test of this solver
    /// asserts, and rightly. Over it, the last moment is not enough time and the walk has to start while
    /// there is still room to choose the route: Pallmagia's 30-yalm circle needed fifteen yalms of walking
    /// and got 0.7 seconds to do it in.
    /// </summary>
    private const float DriftMinEscape = 10f;

    /// <summary>And the most it will walk for something that is not urgent yet, so a huge slow zone does
    /// not send the character across the map for the next twenty seconds.</summary>
    private const float DriftMaxWalk = 30f;

    /// <summary>
    /// Step off ground that will fire, while there is still time to do it cheaply. Returns
    /// <paramref name="fallback"/> (the uptime answer) when the ground underfoot is not going to fire,
    /// when nothing clear is close enough to be worth it, or when the uptime answer already moves further
    /// than the drift would.
    /// </summary>
    private static SafeSpot DriftOffDoomedGround(AIHints hints, DateTime now, WPos player, float cellSize, float margin, UptimeGoal? goal, float moveSpeed, SafeSpot fallback)
    {
        var underfoot = hints.SecondsUntilDangerAt(player, now, margin);
        if (underfoot > DriftLookAhead)
            return fallback;

        // Ground that stays clear for the whole look-ahead, scored the usual way, so the uptime band and
        // the positional still decide between the candidates that qualify.
        if (!TryNearestSafe(hints, now.AddSeconds(DriftLookAhead), now, player, cellSize, margin, goal, moveSpeed, float.MaxValue, out var clear))
            return fallback;
        if (!clear.NeedToMove || !clear.Found)
            return fallback;
        var walk = (clear.Target - player).Length();
        if (walk <= DriftMinEscape || walk > DriftMaxWalk)
            return fallback;

        // Never toward what we are fighting. Leaving early is a convenience, and a convenience move that
        // closes on the target is how a character ends up in the one place this solver exists to refuse:
        // a boss standing in a permanently lethal core, where every cell nearer to it is worse than the
        // ground being left. Outward or sideways only; if the safe ground is inward, the ordinary dodge
        // can have it once the zone is actually imminent.
        if (goal is { } g && (clear.Target - g.Target).Length() < (player - g.Target).Length() - 0.01f)
            return fallback;
        return clear;
    }

    /// <summary>Distinct activation instants inside the horizon, latest first: each is a candidate deadline
    /// for a dodge that clears what lands by then and leaves the rest to the next frames.</summary>
    private static List<DateTime> ActivationsBefore(AIHints hints, DateTime deadline, DateTime now)
    {
        var times = new List<DateTime>();
        foreach (var z in hints.ForbiddenZones)
            if (z.Activation > now && z.Activation < deadline && !times.Contains(z.Activation))
                times.Add(z.Activation);
        times.Sort();
        times.Reverse();
        return times;
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
    /// <para>That last clause is load-bearing, and the case that proves it is a boss standing in the middle
    /// of a permanently lethal disc: melee range is then entirely inside the kill circle, and BossmodReborn's
    /// stick-to-target range walks people into it. Both passes below filter to safe cells before any goal
    /// weighting is applied, so an unreachable goal yields <see cref="SafeSpot.Stay"/> rather than a shorter
    /// walk into the danger. A module only has to publish such ground as a forbidden zone with no activation
    /// time, so it reads as dangerous now rather than at some future resolve.</para>
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
        // Three passes, tightest first. The inside pass is what keeps a side switch off the border: a cell
        // a step past the line is always nearest, and the anchor bonus (6 yd2 of walking) cannot outbid the
        // extra arc at melee radius. Measured on a samurai switching rear to flank, the nearest-on-side
        // pass parked it one cell past the border, and the boss's next turn put it back on the rear. The
        // nearest cell past the floor is still what wins, so the switch stays as short as the floor allows.
        if (Nearest(hints, deadline, player, cellSize, margin, g, SideRule.Inside) is { } deep)
            return deep;
        if (Nearest(hints, deadline, player, cellSize, margin, g, SideRule.OnSide) is { } onSide)
            return onSide;
        return Nearest(hints, deadline, player, cellSize, margin, g, SideRule.Any) ?? SafeSpot.Stay;
    }

    /// <summary>How strictly a regain pass reads the positional.</summary>
    private enum SideRule
    {
        Any,
        OnSide,
        Inside,
    }

    private static SafeSpot? Nearest(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin, UptimeGoal g, SideRule rule)
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
                if (rule == SideRule.OnSide && !g.Satisfied(p))
                    continue;
                if (rule == SideRule.Inside && !g.SatisfiedInside(p))
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
    /// moving: hold, provided the spot underfoot clears the same bar the search used. This is what keeps
    /// the margin-based "need to move" test from twitching forever in arenas too constrained to satisfy
    /// the margin — e.g. a donut whose safe centre is smaller than the margin itself: that search ran with
    /// no margin, so holding is judged with none.
    /// <para>Judged with the margin, not without it, since 2026-09-06. Judged without it, the hold applied
    /// anywhere inside the margin band whenever the clear cell was under a yalm away, so the band the user
    /// configured was worth nothing: a Warrior stood 8.1 to 8.7 yalms from a guessed 8-yalm Body Slam with
    /// a 1.5-yalm margin, held there for two seconds, and the game (radius plus hitbox, plus a snapshot
    /// taken early) hit at 8.5.</para>
    /// </summary>
    private static SafeSpot Settle(AIHints hints, DateTime deadline, WPos player, SafeSpot spot, float cellSize, float margin)
        => (spot.Target - player).LengthSq() <= cellSize * cellSize && !hints.InImminentDanger(player, deadline, margin)
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
    /// How much a yard <i>outside the useful band</i> costs, relative to a yard of travel. Both terms are
    /// squared distances, so this is a pure ratio.
    ///
    /// <para>Was 0.6, on the reasoning that travel should lead. In play that meant the uptime term only ever
    /// broke ties: dodging an AOE from 10y out moved the character to 11y — <i>further</i> from the boss —
    /// because that cell was 1.4y away and nothing outweighed it. Measured on that case, 1 changed nothing,
    /// 2 was the first value that moved at all, 4 reached the edge of melee, and 8 did no better than 4.</para>
    ///
    /// <para>Weighting travel so heavily was over-cautious for a reason that does not hold: cells the
    /// character cannot reach before the AOE lands are already discarded by the time budget, before anything
    /// is scored. Among cells that are both safe and reachable, travel is tidiness rather than safety, and
    /// paying a few yards of it to keep melee range is the trade a melee player wants.</para>
    ///
    /// <para>Safety is unaffected either way: an unsafe cell is discarded before it is scored, so no amount
    /// of uptime can buy one.</para>
    /// </summary>
    private const float UptimeWeight = 4f;

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

    private static bool TryNearestSafe(AIHints hints, DateTime deadline, DateTime now, WPos player, float cellSize, float margin, UptimeGoal? goal, float moveSpeed, float timeBudget, out SafeSpot spot)
    {
        var hasGoals = hints.GoalZones.Count > 0;
        var found = false;
        var best = player;
        var bestCost = float.MaxValue;

        // Cost by route, not by displacement. Everything below scores a cell on what it takes to GET there,
        // so a safe wedge behind a rock is priced with the walk around the rock included, and an unreachable
        // one is not considered at all.
        var grid = new RouteGrid(hints, deadline, player, cellSize, margin, now, moveSpeed);

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
        spot = new SafeSpot(true, true, best, (waypoint - player).Normalized(), waypoint, grid.Route(player, best));
        return true;
    }
}
