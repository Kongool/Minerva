using System;
using System.Collections.Generic;
using System.Numerics;
using Minerva;
using Minerva.Modules;

namespace Minerva.Automation;

/// <summary>
/// Drives the auto-dodge loop: each frame it builds <see cref="AIHints"/> from the active module's
/// components for the local player, runs <see cref="ArenaPathfinder"/>, and exposes the resulting
/// <see cref="SafeSpot"/> for the radar to display. Actual movement is only issued when auto-dodge
/// is explicitly enabled AND a real <see cref="IMovementController"/> is installed — by default it
/// is guidance only (draw the dodge target; the player moves).
/// </summary>
public sealed class AIManager
{
    private readonly WorldState world;
    private readonly ModuleManager modules;
    private readonly Configuration config;
    private readonly IMovementController movement;
    private readonly AIHints hints = new();
    private readonly AutoHints? autoHints;
    private readonly ArenaFootprint footprint = new();

    /// <summary>
    /// Party roles from Daedalus, so role-keyed mechanics resolve. Nothing else in Minerva writes
    /// PartyRolesConfig, so without this every member is Unassigned and tower/tether logic no-ops.
    /// </summary>
    private readonly DaedalusRosterIPC roster = new();
    private readonly List<WPos> knownVoids = [];
    private bool floorProbeStoodDown; // tracks the self-calibration state so the stand-down log fires on transition, not every frame
    private ushort voidsZone;
    private WPos? committedTarget;
    private Positional requestedPositional;
    private DateTime requestedUntil;

    public SafeSpot Current { get; private set; } = SafeSpot.Stay;

    /// <summary>True while the active module says any action would punish the local player (Pyretic-style
    /// "stop everything"). Published over IPC for rotation plugins; see <see cref="MinervaIpc"/>.</summary>
    public bool MustNotAct { get; private set; }

    /// <summary>True while the active module says movement would punish the local player.</summary>
    public bool MustNotMove { get; private set; }

    /// <summary>Where a gaze says to face, when one is active and the current facing is unsafe.</summary>
    public Angle? SafeFacing { get; private set; }

    /// <summary>A gaze is active but every facing is forbidden — worth surfacing rather than hiding.</summary>
    public bool FacingImpossible { get; private set; }

    /// <summary>
    /// True while something constrains which way you may face — i.e. a gaze is live.
    /// <para>This exists for rotation plugins. The game's "face target on action" setting means <em>using
    /// almost any ability turns you toward your target</em>, so a rotation that keeps casting during a gaze
    /// turns you back into it however carefully Minerva just turned you away. A rotation cannot know that
    /// from a status list; it can know it from here.</para>
    /// </summary>
    public bool FacingConstrained { get; private set; }

    /// <summary>Seconds until acting starts being punished; 0 while it already is, NaN when none is known.
    /// <para>Exposed for rotation plugins, which need lead time rather than a present-tense flag — see
    /// <see cref="AIHints.NextMustNotAct"/>.</para></summary>
    public float SecondsUntilMustNotAct { get; private set; } = float.NaN;

    /// <summary>Seconds until movement starts being punished; 0 while it already is, NaN when none.</summary>
    public float SecondsUntilMustNotMove { get; private set; } = float.NaN;

    /// <summary>Seconds until the soonest gaze snapshots facing; NaN when none is pending.</summary>
    public float SecondsUntilGaze { get; private set; } = float.NaN;

    /// <summary>
    /// How long the player can stand still and cast from where they are; <see cref="float.MaxValue"/> when
    /// nothing is pending. What a healer needs before committing an eight second raise.
    /// </summary>
    public float MaxCastTime { get; private set; } = float.MaxValue;
    public AIHints Hints => this.hints;
    public bool HasSolution { get; private set; }

    /// <summary>The installed movement controller (real or null-object), for status/diagnostics.</summary>
    public IMovementController Movement => this.movement;

    public AIManager(WorldState world, ModuleManager modules, Configuration config, IMovementController? movement = null, Minerva.Generation.IShapeResolver? shapes = null)
    {
        this.world = world;
        this.modules = modules;
        this.config = config;
        this.movement = movement ?? new NullMovementController();
        this.autoHints = shapes != null ? new AutoHints(world, shapes) : null;
    }

    /// <summary>How far around the player the unscripted-content dodge considers, with no arena to go on.</summary>
    private const float TrashHorizon = 30f;

    public void Update()
    {
        this.HasSolution = false;
        this.Current = SafeSpot.Stay;
        this.MustNotAct = false;
        this.MustNotMove = false;
        this.SafeFacing = null;
        this.FacingImpossible = false;
        this.FacingConstrained = false;
        this.SecondsUntilMustNotAct = float.NaN;
        this.SecondsUntilMustNotMove = float.NaN;
        this.SecondsUntilGaze = float.NaN;
        this.MaxCastTime = float.MaxValue;

        var module = this.modules.ActiveModule;
        var pc = this.modules.LocalPlayer();
        if (pc == null)
        {
            this.movement.Stop();
            return;
        }

        this.ObserveFootprint(pc);
        this.roster.Update(DateTime.UtcNow);

        if (module != null)
        {
            module.BuildAIHints(0, pc, this.hints, this.roster.AssignmentFor(this.world.Party, pc));
        }
        else if (!this.BuildTrashHints(pc))
        {
            // nothing authored and nothing guessable: stay out of the way rather than invent guidance
            this.movement.Stop();
            return;
        }

        var now = this.world.CurrentTime;
        this.MustNotAct = this.hints.MustNotAct(now);
        this.MustNotMove = this.hints.MustNotMove(now);
        this.SecondsUntilMustNotAct = Countdown(this.hints.NextMustNotAct(now), now);
        this.SecondsUntilMustNotMove = Countdown(this.hints.NextMustNotMove(now), now);
        this.SecondsUntilGaze = Countdown(this.hints.NextGazeResolve(now), now);
        this.MaxCastTime = this.hints.MaxCastTime(now, Math.Clamp(this.config.AutoDodgeSafetyMargin, 0f, 10f), ArenaPathfinder.DefaultMoveSpeed);
        // react earlier (5s look-ahead) and keep the configured clearance from the AOE edge so the dodge
        // actually clears the zone rather than stopping against it
        var margin = Math.Clamp(this.config.AutoDodgeSafetyMargin, 0f, 10f);
        const float horizon = 5f;
        // Give up as little uptime as safety allows, rather than taking the first cell out of the AOE -- and
        // let the role say what uptime means. A tank and a Black Mage do not want the same distance, and
        // scoring both against melee reach is what walks a caster into the boss to save a yard of travel.
        var target = this.UptimeTarget(module, pc);
        UptimeGoal? goal = target != null
            ? UptimeGoal.For(target, pc.Role, this.ActivePositional, Math.Clamp(this.config.PositionalArcMarginDeg, 0f, 44f))
            : null;
        this.Current = ArenaPathfinder.Solve(this.hints, now, horizonSeconds: horizon, safetyMargin: margin, goal: goal);
        this.Current = this.HoldCommitment(pc, now.AddSeconds(horizon), margin);
        this.Current = this.RejectFloorless(pc, this.Current, now, margin, goal, horizon);
        this.HasSolution = true;
        this.ResolveFacing(pc, now.AddSeconds(horizon));

        // a stand-still punisher outranks the dodge: moving is what kills you, so hold position
        if (this.MustNotMove)
        {
            this.Current = SafeSpot.Stay;
            this.movement.Stop();
            return;
        }

        // steering is opt-in and only meaningful with a real controller installed
        if (this.config.AutoDodgeEnabled && this.Current.NeedToMove && this.Current.Found)
            this.movement.MoveTo(this.Current.Steer); // the next point on the route, not the far end of it
        else
            this.movement.Stop();
    }

    /// <summary>Seconds from now, floored at zero. NaN means "no such thing is coming", which a consumer
    /// tests with a single <c>float.IsNaN</c> rather than agreeing on a sentinel.</summary>
    private static float Countdown(DateTime? at, DateTime now)
        => at is { } t ? MathF.Max((float)(t - now).TotalSeconds, 0f) : float.NaN;

    /// <summary>
    /// What to keep uptime on: the boss when a module names one, otherwise whatever the player has
    /// targeted. The second case is the whole of unscripted content, where the dodge previously had no
    /// reason to stay near anything and would happily leave the pull to sidestep a puddle.
    /// </summary>
    private Actor? UptimeTarget(ModuleBase? module, Actor pc)
    {
        if (module?.PrimaryActor is { IsDeadOrDestroyed: false } boss)
            return boss;
        var t = this.world.Actors.Find(pc.TargetID);
        return t is { IsDeadOrDestroyed: false, Type: ActorType.Enemy, IsAlly: false } ? t : null;
    }

    /// <summary>Every place an actor stands is, by definition, ground. Cheapest possible arena survey.</summary>
    private void ObserveFootprint(Actor pc)
    {
        this.footprint.EnterZone(this.world.CurrentZone);
        if (this.voidsZone != this.world.CurrentZone)
        {
            this.voidsZone = this.world.CurrentZone;
            this.knownVoids.Clear(); // a hole in the last arena says nothing about this one
            this.floorProbeStoodDown = false; // re-evaluate the probe fresh in the new zone (fresh stand-down signal)
        }

        var playerY = pc.PosRot.Y;
        foreach (var a in this.world.Actors)
        {
            if (a.IsDeadOrDestroyed || a.Type is not (ActorType.Player or ActorType.Enemy or ActorType.Buddy))
                continue;
            this.footprint.Observe(a.Position, a.PosRot.Y - playerY);
        }
    }

    /// <summary>How many times a frame we will ask the floor and re-solve before giving up and holding.</summary>
    private const int MaxFloorRetries = 3;

    /// <summary>Remembered holes are marked this wide, so the re-solve steps past rather than one cell over.</summary>
    private const float VoidRadius = 2f;

    /// <summary>Zones do not sprout new holes mid-fight; this is only a cap on unbounded growth.</summary>
    private const int MaxRememberedVoids = 64;

    /// <summary>
    /// Refuse a destination the character would fall from, and remember why.
    /// <para>The probe is the backstop the footprint cannot be: an observed extent is a rectangle, and a
    /// rectangle drawn round a donut arena declares the hole in the middle to be solid. Asking the floor
    /// catches that, and catches a gap on the way as well as at the end.</para>
    /// <para>A hole found once is kept as a temporary obstacle for the rest of the zone. It is a fact about
    /// the map rather than about this moment, so the next solve routes round it without another probe, and
    /// the guess converges instead of rediscovering the same ledge every pull.</para>
    /// </summary>
    private SafeSpot RejectFloorless(Actor pc, SafeSpot spot, DateTime now, float margin, UptimeGoal? goal, float horizon)
    {
        if (!spot.NeedToMove || !spot.Found)
            return spot;

        var from = pc.PosRot;

        // Sanity-check the probe against the one spot we know is standable: the one being stood on. If the
        // collision query disagrees with that, it is wrong here -- wrong ray, unloaded geometry, a zone it
        // cannot answer for -- and every cell would read as a hole, which stops the dodge dead. Trusting a
        // broken probe is worse than not having one, so stand down rather than freeze.
        if (!GameSync.GameData.HasFloorAt(from.X, from.Z, from.Y))
        {
            // instrumentation: only log the transition so a live pull can tell "stood down" from "never ran",
            // without spamming a line every frame the probe is miscalibrated for this zone
            if (!this.floorProbeStoodDown)
            {
                Service.Log.Information("Minerva floor probe: STOOD DOWN — no floor detected under the player's own feet (miscalibrated ray or unloaded collision); floor checks disabled until it reads floor again.");
                this.floorProbeStoodDown = true;
            }
            return spot;
        }
        if (this.floorProbeStoodDown)
        {
            Service.Log.Information("Minerva floor probe: RECOVERED — floor detected under the player's feet; floor checks re-enabled.");
            this.floorProbeStoodDown = false;
        }

        for (var attempt = 0; attempt < MaxFloorRetries; ++attempt)
        {
            var to = new Vector3(spot.Target.X, from.Y, spot.Target.Z);
            if (GameSync.GameData.PathHasFloor(new Vector3(from.X, from.Y, from.Z), to))
                return spot;

            // instrumentation: the probe caught a ledge — a discrete, low-frequency event worth a line each
            Service.Log.Information($"Minerva floor probe: REJECTED dodge target {spot.Target} — no floor along the path (attempt {attempt + 1}/{MaxFloorRetries}); re-solving around the void.");
            this.RememberVoid(spot.Target);
            this.hints.TemporaryObstacles.Add(new SDCircle(spot.Target, VoidRadius));
            spot = ArenaPathfinder.Solve(this.hints, now, horizonSeconds: horizon, safetyMargin: margin, goal: goal);
            if (!spot.NeedToMove || !spot.Found)
                return spot;
        }

        // three answers in a row over the void: holding beats walking off, even inside an AOE
        Service.Log.Information("Minerva floor probe: HELD — three floorless answers in a row; holding position rather than dodging off the edge.");
        return SafeSpot.Stay;
    }

    private void RememberVoid(WPos p)
    {
        for (var i = 0; i < this.knownVoids.Count; ++i)
            if ((this.knownVoids[i] - p).LengthSq() < VoidRadius * VoidRadius)
                return;
        if (this.knownVoids.Count < MaxRememberedVoids)
            this.knownVoids.Add(p);
    }

    /// <summary>Re-apply the holes learned so far. Cleared with the footprint on a zone change.</summary>
    private void ApplyKnownVoids()
    {
        for (var i = 0; i < this.knownVoids.Count; ++i)
            this.hints.TemporaryObstacles.Add(new SDCircle(this.knownVoids[i], VoidRadius));
    }

    /// <summary>
    /// Work out where a gaze wants the character pointed, and turn there when asked to.
    /// <para>Facing is its own axis: a gaze does not care where you stand, and the dodge does not care
    /// where you look, so this runs alongside the pathfinder rather than through it. Turning is only
    /// issued when the current facing is actually unsafe, so a correct facing is never nudged.</para>
    /// </summary>
    private void ResolveFacing(Actor pc, DateTime deadline)
    {
        if (this.hints.ForbiddenDirections.Count == 0)
            return;

        this.FacingConstrained = true;

        if (!this.hints.TryFindBestFacing(deadline, pc.Rotation, out var facing, out var gazesHit))
            return;

        // No fully safe heading is a normal late-fight state here, not a failure — Eye to Eye's orbs sit
        // at the arena corners and compress their timings as the fight runs on, until four are looking at
        // once and every heading is covered. Turn to the least-bad one anyway: standing still because
        // nothing is perfect means eating all of them instead of one.
        this.FacingImpossible = gazesHit > 0;

        // already clear: TryFindSafeFacing hands back the preferred facing untouched in that case
        if (facing.AlmostEqual(pc.Rotation, 0.01f))
            return;

        this.SafeFacing = facing;
        if (this.config.AutoFaceGazes)
            this.movement.Face(facing);
    }

    /// <summary>
    /// Ask for a specific side for the next few seconds — a rotation saying "my next GCD wants rear".
    /// <para>Expires on its own rather than needing a matching release: a rotation that swaps target, dies,
    /// or is switched off mid-GCD would otherwise pin the character behind a boss forever. The caller
    /// re-asserts each time it still wants it, which is also what makes a dropped IPC call harmless.</para>
    /// </summary>
    public void RequestPositional(Positional sides, double seconds)
    {
        this.requestedPositional = sides;
        this.requestedUntil = this.world.CurrentTime.AddSeconds(Math.Clamp(seconds, 0d, 30d));
    }

    /// <summary>The side set in force right now: a live rotation request, else the configured preference.</summary>
    private Positional ActivePositional
        => this.requestedUntil > this.world.CurrentTime ? this.requestedPositional : this.config.DesiredPositional;

    /// <summary>How close counts as having arrived, in yards. Below the solver's one-yard cell.</summary>
    private const float ArrivedRange = 0.5f;

    /// <summary>
    /// Keep walking to the spot we already chose, as long as it is still safe and we have not reached it.
    /// <para>The solver is stateless and re-rasterises the arena every frame, so as the player moves the
    /// "nearest safe cell" flips between neighbouring cells and the dodge stutters in place. Worse, a player
    /// standing outside an AOE but inside the safety margin satisfies "must move" and then "may stay" on
    /// alternate frames, which reads in game as pulsing — it looks like the AI cannot tell you are already
    /// clear. Committing to a destination until it is reached or genuinely becomes unsafe removes both.</para>
    /// </summary>
    private SafeSpot HoldCommitment(Actor pc, DateTime deadline, float margin)
    {
        if (!this.Current.NeedToMove || !this.Current.Found)
        {
            this.committedTarget = null;
            return this.Current;
        }

        if (this.committedTarget is { } prev
            && (prev - pc.Position).Length() > ArrivedRange
            && !this.hints.InImminentDanger(prev, deadline, margin))
        {
            return this.Current with { Target = prev, Direction = (prev - pc.Position).Normalized() };
        }

        this.committedTarget = this.Current.Target;
        return this.Current;
    }

    /// <summary>
    /// Hints for content with no module, from enemy cast bars. There is no arena to work with, so the dodge
    /// is given a circle around the player to solve inside — enough room to leave any cast it can see,
    /// without claiming to know where the floor ends.
    /// </summary>
    private bool BuildTrashHints(Actor pc)
    {
        if (!this.config.AutoHintsForTrash || this.autoHints is not { Count: > 0 })
            return false;

        this.hints.Clear();
        this.hints.PlayerPosition = pc.Position;

        // Prefer the arena we have watched people stand in over a window centred on the player. The window
        // is what walks you off a platform: stand on the edge and half of it is over the drop, so the far
        // side of an edge-hugging AOE reads as clear ground. An arena-centred box has no such far side.
        if (this.footprint.TryEstimate(out var center, out var bounds))
        {
            this.hints.Center = center;
            this.hints.Bounds = bounds;
        }
        else
        {
            this.hints.Center = pc.Position;
            this.hints.Bounds = new ArenaBoundsCircle(TrashHorizon);
        }

        this.autoHints.AddForbiddenZones(this.hints);
        this.ApplyKnownVoids();
        return true;
    }
}
