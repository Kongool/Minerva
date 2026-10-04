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
/// How far from the target a role wants to stand: never closer than <see cref="Min"/>, never further than
/// <see cref="Max"/>, and when it has to move, to <see cref="Preferred"/>. All three measured from the target's
/// hitbox edge, the way the game measures ability range, so a large boss does not eat the band.
/// <para>A band rather than a distance because standing still is worth more than standing exactly right: a caster
/// inside [Min, Max] is useful and should be left to cast, and one that has drifted out should come back to
/// Preferred rather than to the edge it crossed, or the next step the boss takes puts it out again.</para>
/// </summary>
public readonly record struct RangeBand(float Min, float Preferred, float Max)
{
    /// <summary>How close to <see cref="Preferred"/> counts as arrived.</summary>
    public const float StopTolerance = 1f;

    /// <summary>The narrowest a band may be, so there is always somewhere inside it to stand.</summary>
    public const float MinWidth = 1f;

    /// <summary>Tanks and melee: on the hitbox, never more than three yalms off it.</summary>
    public static readonly RangeBand Melee = new(0f, 1.5f, 3f);

    /// <summary>Casters, ranged and healers: out of the cleaves, inside the stacks and heals.</summary>
    public static readonly RangeBand Ranged = new(8f, 12f, 15f);

    /// <summary>Tanks share the melee band. Healers and anyone whose role is unknown share the ranged one: standing
    /// too far back costs damage, standing too close costs the pull.</summary>
    public static RangeBand DefaultFor(Role role) => role is Role.Tank or Role.Melee ? Melee : Ranged;

    /// <summary>
    /// This band made consistent: no negatives, at least a yalm wide, and <see cref="Preferred"/> kept a whole
    /// tolerance inside each edge where the band is wide enough for that.
    /// <para>The last part is what stops a walk turning into a twitch. Arriving means being within the tolerance of
    /// Preferred, so a Preferred a hair inside Min would count arriving a hair outside the band as done -- and the
    /// next frame would start the walk again.</para>
    /// </summary>
    public RangeBand Normalized()
    {
        var max = MathF.Max(this.Max, MinWidth);
        var min = Math.Clamp(this.Min, 0f, max - MinWidth);
        var low = min + StopTolerance;
        var high = max - StopTolerance;
        return new(min, low <= high ? Math.Clamp(this.Preferred, low, high) : (min + max) / 2f, max);
    }

    /// <summary>Where a walk to the band stops: within the tolerance of Preferred, and never outside the band.</summary>
    public (float Low, float High) StopWindow
        => (MathF.Max(this.Preferred - StopTolerance, this.Min), MathF.Min(this.Preferred + StopTolerance, this.Max));

    /// <summary>
    /// This band, but never asking a caster to stand farther from the boss than the group does.
    /// <para>A minimum measured from the boss alone runs a caster away from its own party. With the tank and melee
    /// stacked on the boss, every step the boss takes toward the group puts the caster under its minimum, so it walks
    /// out -- and the boss, still being tanked toward the group, follows. The user, 2026-09-25: "ranged and casters
    /// need to stay with the group, not continue to run away from the group to maintain min distance."</para>
    /// <para>So the minimum and the preferred distance are both capped at where the group stands: with the melee on
    /// the boss a caster settles with them and walks back <i>to them</i>, and with the whole group out at range the
    /// band applies as set. The maximum is untouched -- a group standing out of range is not followed out of it.</para>
    /// </summary>
    public RangeBand FollowGroup(float? groupDistance)
    {
        if (groupDistance is not { } g)
            return this;
        var min = MathF.Min(this.Min, g);
        return new(min, MathF.Min(this.Preferred, MathF.Max(g, min)), this.Max);
    }

    /// <summary>
    /// How far the rest of the group stands from the target's hitbox edge: the median, so one member running off to
    /// a spread marker does not drag everybody after it. Null with nobody else to go by.
    /// </summary>
    public static float? GroupDistance(IReadOnlyList<WPos> others, WPos target, float hitboxRadius)
    {
        if (others.Count == 0)
            return null;
        var d = new float[others.Count];
        for (var i = 0; i < d.Length; ++i)
            d[i] = MathF.Max((others[i] - target).Length() - hitboxRadius, 0f);
        Array.Sort(d);
        var mid = d.Length / 2;
        return d.Length % 2 == 1 ? d[mid] : (d[mid - 1] + d[mid]) / 2f;
    }
}

/// <summary>
/// What the dodge aims for when it is not busy avoiding something: stay within <see cref="Range"/> of the
/// target, and prefer the requested side of it.
/// <para>Bundled rather than passed loose because these four always travel together, and a solver taking
/// four bare positional arguments invites the caller to transpose two of them.</para>
/// </summary>
public readonly record struct UptimeGoal(WPos Target, Angle Rotation, float Range, Positional Positional = Positional.Any, float BoundaryMarginDeg = 15f, float MinRange = 0f)
{
    // the two lines that separate the three sides, measured off the target's facing
    private const float FrontFlankEdge = 45f;
    private const float FlankRearEdge = 135f;

    /// <summary>The ring the walk back aims for, as distances from the target's centre: the band's stop window. Left at
    /// zero, the ring is the band itself.</summary>
    public float AimLow { get; init; }

    /// <inheritdoc cref="AimLow"/>
    public float AimHigh { get; init; }

    /// <summary>The ring a walk to the band should end in.</summary>
    public (float Low, float High) Aim => this.AimHigh > 0f ? (this.AimLow, this.AimHigh) : (this.MinRange, this.Range);

    /// <summary>
    /// The goal for keeping a given player useful against a given target.
    /// <para>Roles do not share a definition of uptime, and treating them as if they did is what drags a
    /// Black Mage into a boss's melee band to shave a yard off a dodge. The band comes from the caller's settings;
    /// without one, the role's default.</para>
    /// <para>Melee get no floor even though their band starts at zero: MinRange zero means "anywhere up to the
    /// centre", which is where a melee standing inside a big hitbox already is. A floor at the hitbox edge would
    /// walk them out of it.</para>
    /// </summary>
    public static UptimeGoal For(Actor target, Role role, Positional positional = Positional.Any, float boundaryMarginDeg = 15f, RangeBand? band = null)
    {
        var b = (band ?? RangeBand.DefaultFor(role)).Normalized();
        var r = target.HitboxRadius;
        var (low, high) = b.StopWindow;
        return new(target.Position, target.Rotation, r + b.Max, positional, boundaryMarginDeg, b.Min > 0f ? r + b.Min : 0f)
        {
            AimLow = r + low,
            AimHigh = r + high,
        };
    }

    /// <summary>
    /// How far outside the useful band this point is, in yalms. Zero anywhere inside it.
    /// <para>The distinction matters more than it looks. Scoring raw distance to the target makes the goal
    /// a point attractor — every cell nearer the boss scores better, forever — which is approximately right
    /// for melee and plainly wrong for everyone else. What both roles actually want is a band: inside it,
    /// uptime is satisfied and the dodge should be spending its budget on safety and travel instead.</para>
    /// </summary>
    public float ExcessRange(WPos p)
    {
        var d = (p - this.Target).Length();
        if (d > this.Range)
            return d - this.Range;
        return d < this.MinRange ? this.MinRange - d : 0f;   // too close is out of position too, for a backline job
    }

    /// <summary>How much nearer the band a fresh destination has to be before it replaces one already being walked
    /// to: a cell. Any less and two neighbouring cells that both sit in the band could trade places every frame.</summary>
    private const float ReplanGain = 1f;

    /// <summary>
    /// Whether a destination chosen earlier has stopped being the way to this goal: <paramref name="fresh"/> is more
    /// than a cell nearer the band than <paramref name="committed"/>.
    /// <para>A destination is committed to so the walk does not re-pick a cell every frame, and it was kept for as
    /// long as it stayed safe and unreached -- whether or not it still led anywhere. Cresceregina, 2026-09-26: Korha,
    /// 56y out and off the floor, was given the nearest rim cell as his way in, 30y from the boss. He was walked to
    /// the boss by hand, and when the keys were let go the commitment walked him back out to that rim cell, then in
    /// again. A boss that walks off does the same thing to a spot that was in the band when it was chosen.</para>
    /// </summary>
    public bool Supersedes(WPos fresh, WPos committed) => this.ExcessRange(committed) > this.ExcessRange(fresh) + ReplanGain;

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

    /// <summary>How far past the horizon a dodge's destination should preferably stay clear, in seconds. About one
    /// wave interval of the staggered mechanics seen so far (Blot's columns are two seconds apart).</summary>
    private const float LookPastHorizon = 3f;

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
        // Standing off the floor is a reason to move even when nothing is aimed at you. A knockback puts
        // people outside the arena several times a fight, and until now the solve looked at that character,
        // found no AOE on them and said stay: Quaqua, 2026-09-20, three and a third yalms outside the floor
        // for at least eight seconds with the dodge reporting a spot found and no need to reach it. The
        // grid already knows how to route in from outside -- it prices outside cells and floods back
        // through them -- so all that was missing was a reason to ask it.
        var offTheFloor = !hints.Bounds.Contains(hints.Center, player);

        if (!offTheFloor && !hints.InImminentDanger(player, deadline, safetyMargin) && !hints.Misplaced(player))
        {
            // Nothing is about to land -- but if the ground underfoot is going to fire at all, leaving now
            // is the cheap version of leaving later. Pallmagia, 2026-09-06: the character stood in a
            // 30-yalm circle for eight seconds because it was not yet imminent, then had to cross the arena
            // with 0.7s to spare and was caught by a cone on the way. The user's call, and the right one:
            // "i would leave earlier, it could cost the run if something lethal was inside of those aoe's".
            var uptime = Regain(hints, deadline, player, cellSize, safetyMargin, goal, solveNow, moveSpeed);
            return DriftOffDoomedGround(hints, solveNow, player, cellSize, safetyMargin, goal, moveSpeed, uptime);
        }

        // Prefer somewhere we can actually reach before the cast lands. If nothing qualifies, widen rather
        // than freeze: partially clearing a zone beats standing in it because the ideal spot was too far.
        var budget = TimeUntilDanger(hints, player, deadline, solveNow);

        // Among places to go, prefer one that is still clear a little past the horizon. Forbidden Folios,
        // 2026-09-14: leaving a column of circles, the nearer side was the next column to fire, 1.5s past the
        // horizon, so it scored the same as the far side -- and once it entered the horizon, every way off it
        // crossed the column that fired first. Only where to go changes; nothing past the horizon becomes a reason
        // to move, and where nothing reachable stays clear that long, the horizon alone decides as before.
        if (TryNearestSafe(hints, deadline.AddSeconds(LookPastHorizon), solveNow, now, player, cellSize, safetyMargin, goal, moveSpeed, budget, out var spot))
            return Settle(hints, deadline, player, spot, cellSize, safetyMargin);
        if (TryNearestSafe(hints, deadline, solveNow, now, player, cellSize, safetyMargin, goal, moveSpeed, budget, out spot))
            return Settle(hints, deadline, player, spot, cellSize, safetyMargin);

        // The lead is clearance to spare, and it came off the budget above. Before settling for a stage, take ground
        // clear of the whole horizon that can still be reached in real time, by a walk nothing fires on first.
        // Forbidden Folios, 2026-09-14: clear of the next column of circles and inside the one after, with a gap clear
        // of both 14 yalms away and 3.2s to walk it. Minus the lead, the budget was 13 yalms, so the stage below
        // answered "you are clear of the next column, stay" -- a hold that protected from nothing -- and the walk out
        // started two seconds later and fell 3.9 yalms short. A hold is right when leaving means crossing the wave
        // about to fire (the cone waves below); the walk check keeps that case a hold.
        if (clearanceLead > 0f
            && TryNearestSafe(hints, deadline, solveNow, now, player, cellSize, safetyMargin, goal, moveSpeed, TimeUntilDanger(hints, player, deadline, now), out spot)
            && RouteIsClear(hints, player, spot, solveNow, safetyMargin, moveSpeed))
            return Settle(hints, deadline, player, spot, cellSize, safetyMargin);

        // Nothing reachable is clear of the whole horizon. Before considering somewhere further away, ask
        // for a spot that is reachable and clear of everything up to an earlier wave: surviving the cast
        // that is about to land beats standing in it while walking to a cell that answers the next one too.
        //
        // Dark Artistry, 2026-09-06. Long-dead Explorers explode in waves three seconds apart, and each
        // wave turns risky four seconds out, so from the moment the second wave appears the horizon holds
        // two overlapping grids of eight-yalm circles. At 152.27s the character had been walking to a spot
        // for two and a half seconds with the blast 0.6s away; the second wave armed, that spot stopped
        // being clear of everything, and this fell through to the unlimited search, which answered with a
        // cell sixteen yalms away -- nearly three seconds of walking. It was caught in transit, three times
        // in one pull. A person steps out of the circle that is about to go off and deals with the next one
        // next, which is exactly what a nearer deadline asks for.
        //
        // A stage has to protect from something: the character is in its danger, or it is a hold and the walk to ground
        // clear of everything would cross the stage (the cone waves). Lost on the Wind, 2026-09-14: out of reach of the
        // only ground clear of Wind Blade, two small wind arcs the character was nowhere near were offered as stages,
        // and the answers were "stand in the cone" and "walk to the middle of it". Skipping them leaves the search
        // below to head for the safe ground.
        bool? leavingIsClear = null;
        foreach (var soon in ActivationsBefore(hints, deadline, now))
        {
            if (!TryNearestSafe(hints, soon, solveNow, now, player, cellSize, safetyMargin, goal, moveSpeed, budget, out spot))
                continue;
            var staged = Settle(hints, soon, player, spot, cellSize, safetyMargin);
            if (LeavesTimeToEscape(hints, staged.NeedToMove ? staged.Target : player, soon, deadline, now, safetyMargin, moveSpeed, clearanceLead, cellSize)
                && (staged.NeedToMove ? hints.InImminentDanger(player, soon, safetyMargin) : !(leavingIsClear ??= LeavingIsClear())))
                return staged;
        }

        bool LeavingIsClear()
            => TryNearestSafe(hints, deadline, solveNow, now, player, cellSize, safetyMargin, goal, moveSpeed, float.MaxValue, out var clear)
                && RouteIsClear(hints, player, clear, solveNow, safetyMargin, moveSpeed);

        if (TryNearestSafe(hints, deadline, solveNow, now, player, cellSize, safetyMargin, goal, moveSpeed, float.MaxValue, out spot))
            return Settle(hints, deadline, player, spot, cellSize, safetyMargin);
        if (safetyMargin > 0f && TryNearestSafe(hints, deadline, solveNow, now, player, cellSize, 0f, goal, moveSpeed, float.MaxValue, out spot))
            return Settle(hints, deadline, player, spot, cellSize, 0f);

        // Nothing is safe from everything inside the horizon. Before giving up, ask the same question with
        // a nearer deadline: the latest activation for which some reachable cell is clear of all that lands
        // by then. Alexander's Divine Arrow (2026-09-06): two waves of lines two seconds apart covered the
        // whole floor within the five-second horizon, and the dodge stood in the first wave for three
        // seconds reporting "no safe spot" while a cell four yalms away was clear of it. Every frame
        // re-solves, so the second wave is dodged from wherever the first was dodged to, as a person does.
        //
        // The same test as the stages above: an answer that holds the character inside something firing right after
        // its stage protects from nothing, and without the safety margin a cell may exist that clears both. The
        // Ghimlyt Dark, 2026-09-29: with the margin the latest stage was a hold 0.05s before Angry Salamander, 2.5
        // yalms inside it; without, three yalms east cleared the line and the tanks. The first answer found is kept
        // for when nothing better turns up, so this never ends worse than it used to.
        SafeSpot? firstStage = null;
        DateTime firstStageAt = default;
        foreach (var margin in safetyMargin > 0f ? [safetyMargin, 0f] : new[] { safetyMargin })
        {
            foreach (var earlier in ActivationsBefore(hints, deadline, now))
            {
                if (!TryNearestSafe(hints, earlier, solveNow, now, player, cellSize, margin, goal, moveSpeed, float.MaxValue, out spot))
                    continue;
                var settled = Settle(hints, earlier, player, spot, cellSize, margin);
                if (firstStage == null)
                {
                    firstStage = settled;
                    firstStageAt = earlier;
                }
                if (LeavesTimeToEscape(hints, settled.NeedToMove ? settled.Target : player, earlier, deadline, now, margin, moveSpeed, clearanceLead, cellSize))
                    return settled;
            }
        }
        if (firstStage is { } stage)
            return Doorway(hints, firstStageAt, deadline, now, player, cellSize, safetyMargin, moveSpeed) ?? stage;

        // A positioning instruction is advice, danger is not. If nothing honours both, answer the danger
        // alone, as though the module had never asked: a spot to stand on is never worth a hit.
        if (hints.PositioningZones.Count != 0 && !hints.PositioningSuspended)
        {
            hints.PositioningSuspended = true;
            try
            {
                return Solve(hints, now, horizonSeconds, cellSize, safetyMargin, goal, moveSpeed, clearanceLead);
            }
            finally
            {
                hints.PositioningSuspended = false;
            }
        }

        // Nothing on the floor the dodge keeps to is clear. If it keeps a margin off a barrier, try again on half of it: the
        // margin is room to stop in, and a certain hit is worse than standing a yalm from the wall. Metamorph's Cyclone
        // Crossing, 2026-10-03, twice: four 17.5y Wind Spheres and a 16-wide cross left clear ground only past 23.2y of a
        // 25y floor, outside the 23 the dodge keeps to, and all four toons stood 22.5y out and took it. In the three
        // crosses of 09-25 and 09-26, before the margin, they stood at 24.3-24.8y and nothing touched them.
        if (hints.SpareBounds is { } spare && !ReferenceEquals(hints.Bounds, spare))
        {
            var kept = hints.Bounds;
            hints.Bounds = spare;
            try
            {
                return Solve(hints, now, horizonSeconds, cellSize, safetyMargin, goal, moveSpeed, clearanceLead);
            }
            finally
            {
                hints.Bounds = kept;
            }
        }

        // Nothing can be reached clean in time, by this clock. Standing still is the one answer certain to be hit, so head
        // for the nearest ground that is clear when it all lands, across whatever lies between, and let the rotation
        // plugin sprint for it (DodgeSprint). Pallmagia's Occult Missile, 2026-10-03: Xia stood at the arena's edge with
        // four missiles boxing her in; the clear ground was 11.5y away across one of them, 3.7s out, and under her box's
        // lead and margin no route counted as clean, so she stood for the whole 3.7s and took two of them -- as did Rosa.
        // Only reached when every stage and fallback above found nothing, so a pocket that is safe to wait in (Alabaster
        // Blade's) is still kept.
        if (NearestClearGround(hints, deadline, solveNow, now, player, cellSize, moveSpeed, out var clear))
            return new SafeSpot(true, true, clear, (clear - player).Normalized());

        return new SafeSpot(true, false, player, default); // nowhere in the arena is clear
    }

    /// <summary>The nearest ground clear of everything landing by <paramref name="deadline"/>, by straight distance,
    /// whether or not it can be reached in time: the last resort when every clean route has been ruled out.</summary>
    private static bool NearestClearGround(AIHints hints, DateTime deadline, DateTime solveNow, DateTime now, WPos player, float cellSize, float moveSpeed, out WPos best)
    {
        var grid = new RouteGrid(hints, deadline, player, cellSize, 0f, solveNow, moveSpeed, now);
        best = player;
        var bestDistance = float.MaxValue;
        for (var gz = 0; gz < grid.Height; ++gz)
        {
            for (var gx = 0; gx < grid.Width; ++gx)
            {
                if (grid.Blocked(gx, gz) || grid.Outside(gx, gz))
                    continue;
                var p = grid.Center(gx, gz);
                if (hints.InImminentDanger(p, deadline, 0f))
                    continue;
                var d = (p - player).LengthSq();
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }
        }
        return bestDistance < float.MaxValue;
    }

    /// <summary>
    /// Where to wait for <paramref name="soon"/> when no wait leaves time to get out of what fires after it: of the
    /// ground clear of everything firing by then and reachable before it, the spot nearest ground clear of the next
    /// wave. A person waits at the door they will run through. Null when nothing fires after <paramref name="soon"/>
    /// inside the horizon, or no such spot exists.
    /// <para>Page 16, 2026-10-03: Blot in three columns of 15y circles, two seconds apart. Xia waited for the first
    /// wave 2.7y past its edge, where it happened to leave her, inside the second; after the first fired she had 13y to
    /// cross in 2.1s, walked it at full speed and was hit 0.3y short, as was Rosa. The plain wait is the nearest
    /// clear ground, wherever that leaves the way out.</para>
    /// </summary>
    private static SafeSpot? Doorway(AIHints hints, DateTime soon, DateTime deadline, DateTime now, WPos player, float cellSize, float margin, float moveSpeed)
    {
        var next = FirstWaveOn(hints, player, soon, deadline, 0f);
        if (next == DateTime.MaxValue)
            return null;

        foreach (var m in margin > 0f ? [margin, 0f] : new[] { margin })
        {
            var exits = ExitsAfter(hints, soon, next, now, m, cellSize);
            if (exits.Count == 0)
                continue;

            // the walk to the door is short and along clear ground, so it is timed by the clock, not the solve's lead:
            // with the lead, the last second before the wave left no time to take even a step towards it
            var reach = moveSpeed * MathF.Max((float)(soon - now).TotalSeconds - DoorwaySlackSeconds, 0f);
            var best = player;
            var bestScore = hints.InImminentDanger(player, soon, m) ? float.MaxValue : NearestOf(exits, player);
            var r = hints.Bounds.Radius;
            for (var x = -r; x <= r; x += cellSize)
            {
                for (var z = -r; z <= r; z += cellSize)
                {
                    var door = hints.Center + new WDir(x, z);
                    var walk = (door - player).Length();
                    if (walk > reach || !hints.Bounds.Contains(hints.Center, door) || hints.InObstacle(door) || hints.InImminentDanger(door, soon, m))
                        continue;
                    var score = NearestOf(exits, door) + DoorwayWalkWeight * walk;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = door;
                    }
                }
            }
            if (bestScore == float.MaxValue)
                continue;
            return (best - player).LengthSq() <= cellSize * cellSize * 0.25f
                ? SafeSpot.Stay
                : new SafeSpot(true, true, best, (best - player).Normalized());
        }
        return null;
    }

    /// <summary>How long before the wave the walk to the door has to be done.</summary>
    private const float DoorwaySlackSeconds = 0.5f;

    /// <summary>What a yalm walked to the door costs against a yalm saved on the way out after it: there is time
    /// before the wave to walk, and none after it, so the walk is cheap, but not free.</summary>
    private const float DoorwayWalkWeight = 0.1f;

    /// <summary>Clear of the next wave (what fires after <paramref name="soon"/>, up to <paramref name="next"/>) and of
    /// whatever already stands on the ground. The wave at <paramref name="soon"/> will have fired: its ground is open.</summary>
    private static bool ClearOfNextWave(AIHints hints, WPos p, DateTime soon, DateTime next, DateTime now, float margin)
    {
        foreach (var z in hints.ForbiddenZones)
            if ((z.Activation <= now || z.Activation > soon && z.Activation <= next) && AIHints.Touches(z, p, margin))
                return false;
        return true;
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
        if (!TryNearestSafe(hints, now.AddSeconds(DriftLookAhead), now, now, player, cellSize, margin, goal, moveSpeed, float.MaxValue, out var clear))
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
        if (goal is { } g)
        {
            if ((clear.Target - g.Target).Length() < (player - g.Target).Length() - 0.01f)
                return fallback;

            // And it must not throw away uptime the character currently has. Leaving early is worth a walk
            // when you are already out of position -- Pallmagia, a warrior twenty yalms out of melee -- but
            // a ranged toon standing correctly in its band should not answer a telegraph by leaving the
            // fight. Reported 2026-09-06: "ranged toons ran away from the boss at the start".
            if ((player - g.Target).Length() <= g.Range && (clear.Target - g.Target).Length() > g.Range)
                return fallback;
        }

        return clear;
    }

    /// <summary>Distinct activation instants inside the horizon, latest first: each is a candidate deadline
    /// for a dodge that clears what lands by then and leaves the rest to the next frames.
    /// <para>Counted from the real clock, not the lead-shifted one the rest of the solve uses. The lead makes a
    /// zone count as landed early so the character leaves early; it must not also stop the zone being a stage.
    /// Quarried Away, 2026-09-13: two waves of cones three seconds apart covered every direction. Korha stood
    /// in a gap of the first wave with 0.97s left, inside a second-wave cone. With a one-second lead the first
    /// wave's activation fell before the shifted now, so it was never offered as a stage, and the last-resort
    /// search sent the character twelve yalms through a first-wave cone. Counted from now, the first wave is
    /// a stage, the gap is clear of it, and the answer is to hold until it fires.</para></summary>
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
    /// A stage -- ground clear of everything that fires by <paramref name="soon"/> -- is only a plan if, once that wave
    /// has fired, the character can still walk out of whatever else it stands in at <paramref name="at"/> (where the stage
    /// puts it: the spot it walks to, or where it holds) before that fires. Stages exist for waves: clear of the first,
    /// inside the second, deal with the first and then leave. With nothing but a moment between them there is no "then".
    /// <para>The Ghimlyt Dark, 2026-09-29: clear of two Ceruleum Tank bursts due in 2.05s, 2.5 yalms inside Angry
    /// Salamander due in 2.10s. The stage at 2.05s ignored the line, found the spot clear, and held there for two
    /// seconds (earlier, it walked one yalm along the line); it was hit, and the late walk out ran into the next pair
    /// of tanks.</para>
    /// </summary>
    private static bool LeavesTimeToEscape(AIHints hints, WPos at, DateTime soon, DateTime deadline, DateTime now, float margin, float moveSpeed, float lead, float cellSize)
    {
        // The wave to get out of is the first that lands where the stage puts the character, not merely the next to land
        // anywhere: Elm Gigas, 2026-10-04, the next was a rolling puddle's step 0.8s on, elsewhere, and the hold passed with
        // Saar inside two Inspirited Cyclones landing 0.1s after that.
        var next = FirstWaveOn(hints, at, soon, deadline, margin);
        if (next == DateTime.MaxValue)
            return true;
        // the way out is to ground clear of that wave, not out of each of its zones in turn: Page 16, 2026-10-03, the
        // nearest edge of one of Blot's circles was inside the next circle of the column, or off the floor
        var reach = ((float)(next - soon).TotalSeconds - lead) * moveSpeed;
        if (reach < 0f)
            return false;
        for (var x = -reach; x <= reach; x += cellSize)
        {
            for (var z = -reach; z <= reach; z += cellSize)
            {
                var p = at + new WDir(x, z);
                if (x * x + z * z <= reach * reach && hints.Bounds.Contains(hints.Center, p) && !hints.InObstacle(p)
                    && ClearOfNextWave(hints, p, soon, next, now, margin))
                    return true;
            }
        }
        return false;
    }

    /// <summary>The first activation after <paramref name="soon"/>, inside the horizon, of a zone that touches
    /// <paramref name="at"/>; <see cref="DateTime.MaxValue"/> when none does.</summary>
    private static DateTime FirstWaveOn(AIHints hints, WPos at, DateTime soon, DateTime deadline, float margin)
    {
        var first = DateTime.MaxValue;
        foreach (var z in hints.ForbiddenZones)
            if (z.Activation > soon && z.Activation <= deadline && z.Activation < first && AIHints.Touches(z, at, margin))
                first = z.Activation;
        return first;
    }

    /// <summary>Cells of the floor clear of the next wave once <paramref name="soon"/> has fired (<see cref="ClearOfNextWave"/>).</summary>
    private static List<WPos> ExitsAfter(AIHints hints, DateTime soon, DateTime next, DateTime now, float margin, float cellSize)
    {
        var exits = new List<WPos>();
        var r = hints.Bounds.Radius;
        for (var x = -r; x <= r; x += cellSize)
        {
            for (var z = -r; z <= r; z += cellSize)
            {
                var p = hints.Center + new WDir(x, z);
                if (hints.Bounds.Contains(hints.Center, p) && !hints.InObstacle(p) && ClearOfNextWave(hints, p, soon, next, now, margin))
                    exits.Add(p);
            }
        }
        return exits;
    }

    private static float NearestOf(List<WPos> points, WPos from)
    {
        var bestSq = float.MaxValue;
        foreach (var p in points)
            bestSq = MathF.Min(bestSq, (p - from).LengthSq());
        return bestSq == float.MaxValue ? float.MaxValue : MathF.Sqrt(bestSq);
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
    private static SafeSpot Regain(AIHints hints, DateTime deadline, WPos player, float cellSize, float margin, UptimeGoal? goal, DateTime now = default, float moveSpeed = 0f)
    {
        if (goal is not { } g || g.Range <= 0f)
            return SafeSpot.Stay;
        var standing = (player - g.Target).Length();
        if (standing <= g.Range && standing >= g.MinRange && g.Satisfied(player))
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
        // Aim for the preferred ring, not the nearest edge of the band. The nearest cell that restores a band sits
        // exactly on its edge, and the boss taking one step then puts the character back out -- the micro-adjustment
        // loop a caster cannot cast through (reported 2026-09-19). From inside a floor the nearest cell of the ring
        // lies straight out from the target's centre, so "too close" walks directly away without a case of its own.
        var (aimLow, aimHigh) = g.Aim;
        var aim = g with { MinRange = aimLow, Range = aimHigh };
        var pick = Nearest(hints, deadline, player, cellSize, margin, aim, SideRule.Inside)
            ?? Nearest(hints, deadline, player, cellSize, margin, aim, SideRule.OnSide)
            ?? Nearest(hints, deadline, player, cellSize, margin, aim, SideRule.Any);
        if (pick is not { } back)
            return SafeSpot.Stay;

        // Uptime is never worth walking through a telegraph. The destination was already filtered for
        // safety, but nothing looked at the ground in between, so the walk back to the boss after a
        // knockback went straight through an AOE (reported 2026-09-06, and the Elm Gigas Topaz Ray at
        // 224.1s is the same thing). Standing still costs a few seconds of damage; the crossing costs the
        // pull. The next frame re-solves, so as soon as the way clears the walk resumes on its own.
        if (back.NeedToMove && back.Found && now != default
            && !hints.WalkIsClear(player, back.Target, now, margin, moveSpeed))
            return SafeSpot.Stay;
        return back;
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
                var reachOf = (p - g.Target).Length();
                if (reachOf > g.Range || reachOf < g.MinRange)
                    continue;                                  // only cells that actually restore the band
                if (rule == SideRule.OnSide && !g.Satisfied(p))
                    continue;
                if (rule == SideRule.Inside && !g.SatisfiedInside(p))
                    continue;
                if (!hints.Bounds.Contains(center, p))
                    continue;
                if (hints.InImminentDanger(p, deadline, margin) || hints.Misplaced(p))
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

        // Within a cell of the best cell is close enough -- but only for a character already in the ring. One short
        // of it is still out of position, and with a preferred distance that matters: the walk would end a yalm
        // outside the window it was aiming for, and never finish.
        var standing = (player - g.Target).Length();
        var inRing = standing >= g.MinRange && standing <= g.Range;
        return (best - player).LengthSq() > cellSize * cellSize || !inRing
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
    /// <summary>Is every leg of the spot's route walkable before anything on it fires? Each leg is timed from when
    /// the walk reaches its start.</summary>
    private static bool RouteIsClear(AIHints hints, WPos player, SafeSpot spot, DateTime now, float margin, float moveSpeed)
    {
        var from = player;
        var elapsed = 0f;
        foreach (var corner in spot.Route ?? [spot.Target])
        {
            if (!hints.WalkIsClear(from, corner, now.AddSeconds(elapsed), margin, moveSpeed))
                return false;
            elapsed += moveSpeed > 0f ? (corner - from).Length() / moveSpeed : 0f;
            from = corner;
        }

        return true;
    }

    private static SafeSpot Settle(AIHints hints, DateTime deadline, WPos player, SafeSpot spot, float cellSize, float margin)
        => (spot.Target - player).LengthSq() <= cellSize * cellSize && !hints.InImminentDanger(player, deadline, margin) && !hints.Misplaced(player)
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
    /// Room enough. Past this the cell is simply safe and extra space buys nothing, so a fight with an open
    /// floor scores exactly as it did before this existed. Four yalms is about what survives a step of
    /// server lag, the character's own hitbox, and an AOE that turns out a little larger than drawn — the
    /// three ways a cell that measured clear still lands a hit.
    /// </summary>
    private const float ClearanceFloor = 4f;

    /// <summary>
    /// What a missing yalm of clearance costs, against a yalm outside the uptime band
    /// (<see cref="UptimeWeight"/>, 4). Half of it, deliberately: this is a tie-break among cells that are
    /// already safe, not a reason to give up range.
    ///
    /// <para>The ceiling is set by Pallmagia's Roulette, where the kill circle is 5 yalms and melee range
    /// reaches 6.1, so the only place a melee can stand is a one-yalm sliver with almost no clearance in it.
    /// Solving the cost function there, the chosen distance is (26.4 + 9w)/(5 + w), which passes 6.85 —
    /// the point where melee range is lost — at w = 3.6. Anything at or above that trades the whole band
    /// for room and fails that fight.</para>
    ///
    /// <para>The floor is Accept No Imitators, where a tank on a 3-yalm hitbox has a 5.6-yalm band and the
    /// rotating wedge is about a quarter of the radius wide: one yalm of room at four out, 1.5 at the band
    /// edge. Any weight above about 1 walks the character out of the narrowest part to the edge of its
    /// band, which is as far as uptime allows.</para>
    ///
    /// <para>Be honest about the limit, because the two fights above are in direct conflict and the
    /// measurements are close. Escaping into Accept No Imitators' wedge, the nearest way in sits four yalms
    /// out with 1.0 of room and the next one along sits six yalms out with 1.6; preferring the second needs
    /// about 2.9, and Pallmagia starts failing at 3.0. There is no value that fixes the wedge without
    /// throwing away melee range on the Roulette.</para>
    ///
    /// <para>So this is a tie-break and nothing more: at 2 it decides between cells of comparable cost and
    /// never buys room with range. What Accept No Imitators actually wants is for the character to orbit
    /// with the rotating gap rather than stand anywhere in it, which is a per-mechanic behaviour and not a
    /// scoring constant. That fight is still open.</para>
    /// </summary>
    private const float ClearanceWeight = 2f;

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

    private static bool TryNearestSafe(AIHints hints, DateTime deadline, DateTime now, DateTime realNow, WPos player, float cellSize, float margin, UptimeGoal? goal, float moveSpeed, float timeBudget, out SafeSpot spot)
    {
        var hasGoals = hints.GoalZones.Count > 0;
        var found = false;
        var best = player;
        var bestCost = float.MaxValue;

        // Cost by route, not by displacement. Everything below scores a cell on what it takes to GET there,
        // so a safe wedge behind a rock is priced with the walk around the rock included, and an unreachable
        // one is not considered at all.
        var grid = new RouteGrid(hints, deadline, player, cellSize, margin, now, moveSpeed, realNow);

        for (var gz = 0; gz < grid.Height; ++gz)
        {
            for (var gx = 0; gx < grid.Width; ++gx)
            {
                // Outside as well as blocked: the cell underfoot is force-unblocked so a character who is
                // already off the floor can be routed at all, and without this it then qualifies as the
                // nearest safe place to stand -- the answer that stranded people outside the arena.
                if (grid.Blocked(gx, gz) || grid.Outside(gx, gz) || grid.Risky(gx, gz))
                    continue;

                var p = grid.Center(gx, gz);
                if (float.IsInfinity(grid.CostAt(gx, gz)))
                    continue;                                   // nothing walks there from here

                // Safe ground behind something that fires before the walk is across it is not safe ground: getting
                // there is the hit. The route prices such a step, but only to choose among routes -- the cell was still
                // taken, scored on distance alone. Alabaster Blade, 2026-09-26: boxed into a pocket by four lines firing
                // in 0.7s, with a tornado dropped on the pocket firing in 4.7s, the dodge walked thirteen yalms across a
                // line and was mid-line when it went off. Refusing it here leaves the stage below to answer "the lines
                // first": stay in the pocket, then leave the tornado with seconds to spare.
                if (grid.WalksIntoAHit(gx, gz))
                    continue;
                if (hints.Misplaced(p))
                    continue;                                   // not where the module asked us to stand

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

                // Prefer ground with room around it. Everything reaching this line already passed the
                // margin, so this is not a safety test — it is the difference between clear and barely.
                var shortfall = ClearanceFloor - hints.ClearanceAt(p, deadline, ClearanceFloor);
                if (shortfall > 0f)
                    cost += shortfall * shortfall * ClearanceWeight;

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

        // Steer at the route's first corner. This used to be the furthest path cell with no wall in the way, which
        // cut through the same puddles the route keeps clear of. The cell the player stands in can be kept as a
        // corner of its own; steering at that would be steering at your feet.
        var route = grid.Route(player, best);
        var waypoint = route[^1];
        foreach (var corner in route)
        {
            if ((corner - player).Length() > cellSize * 0.5f)
            {
                waypoint = corner;
                break;
            }
        }
        spot = new SafeSpot(true, true, best, (waypoint - player).Normalized(), waypoint, route);
        return true;
    }
}
