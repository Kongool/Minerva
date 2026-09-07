using System;
using System.Collections.Generic;
using System.Numerics;
using Minerva;
using Minerva.GameSync;
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
    private readonly KnownGround knownGround = new(); // where anyone has stood: the probe cannot overrule it
    private int probeRejectStreak;       // consecutive frames on which the probe rejected a target
    private DateTime probeLastReject;
    private bool floorProbeDistrusted;   // the probe refused every frame on this zone: off until the next one
    private bool floorProbeStoodDown; // tracks the self-calibration state so the stand-down log fires on transition, not every frame
    private ushort voidsZone;
    private WPos? committedTarget;
    private Positional requestedPositional;
    private DateTime requestedUntil;
    private DateTime holdUntil;

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
    /// <para>This is the geometric budget from <see cref="AIHints.MaxCastTime"/> <em>and</em> our own intent
    /// to reposition: it reads 0 on any frame the dodge is steering the character. Danger is not the only
    /// reason we move — <c>Regain</c> walks back to uptime with nothing dangerous anywhere — and a consumer
    /// cannot cast through being walked whatever the ground is doing.</para>
    /// </summary>
    public float MaxCastTime { get; private set; } = float.MaxValue;
    /// <summary>
    /// Party roles, from Daedalus's LAN roster. Null reads as everyone unassigned.
    /// <para>Minerva inherited BossmodReborn's role-consuming half and none of the producing half, so
    /// every module's <c>AddAIHints</c> has always been handed <c>Unassigned</c> and anything keyed on a
    /// role — tower soaks, tether pairs, "G1 left, G2 right" — silently never resolved.</para>
    /// </summary>
    public DaedalusRosterIPC? Roster { get; set; }

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

    /// <summary>The game's Heavy debuff: movement speed down by 40%.</summary>
    private const uint HeavyStatus = 14;

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
            this.Publish(this.Decide(false, false, default, DodgeReason.None, DodgeBlocker.NoModule, false));
            return;
        }

        var now = this.world.CurrentTime;
        this.MustNotAct = this.hints.MustNotAct(now);
        this.MustNotMove = this.hints.MustNotMove(now);
        this.SecondsUntilMustNotAct = Countdown(this.hints.NextMustNotAct(now), now);
        this.SecondsUntilMustNotMove = Countdown(this.hints.NextMustNotMove(now), now);
        this.SecondsUntilGaze = Countdown(this.hints.NextGazeResolve(now), now);
        // Heavy is a real 40% cut (3.6y/s measured against 6.0 on the 2026-09-05 Pallmagia recording); a
        // solve that still assumes full speed picks spots the character cannot reach and calls it steering
        var moveSpeed = pc.FindStatus(HeavyStatus) != null ? ArenaPathfinder.DefaultMoveSpeed * 0.6f : ArenaPathfinder.DefaultMoveSpeed;
        this.MaxCastTime = this.hints.MaxCastTime(now, Math.Clamp(this.config.AutoDodgeSafetyMargin, 0f, 10f), moveSpeed);
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
        // A zone hazard adds to an encounter rather than replacing it, so this runs after the boss
        // module has filled its zones -- and runs even when there is no boss, which is the whole point:
        // a field effect with no encounter attached has nowhere else to be expressed. Contained, because
        // a zone module lives for the entire visit and one that throws would otherwise do so every frame.
        if (this.modules.ActiveZoneModule is { } zone)
        {
            try
            {
                zone.CalculateAIHints(0, pc, this.hints);
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, $"Minerva: zone module {zone.GetType().Name} threw building AI hints.");
            }
        }

        var lead = Math.Clamp(this.config.AutoDodgeClearanceLead, 0f, 5f);
        this.Current = ArenaPathfinder.Solve(this.hints, now, horizonSeconds: horizon, safetyMargin: margin, goal: goal, moveSpeed: moveSpeed, clearanceLead: lead);
        // the hold has to judge safety on the same clock the solve did, or it keeps a target the solve rejected
        this.Current = this.HoldCommitment(pc, now.AddSeconds(horizon + lead), margin);
        this.Current = this.RejectFloorless(pc, this.Current, now, margin, goal, horizon);
        this.HasSolution = true;
        this.ResolveFacing(pc, now.AddSeconds(horizon));

        // a stand-still punisher outranks the dodge: moving is what kills you, so hold position
        if (this.MustNotMove)
        {
            this.Publish(this.Decide(this.Current.NeedToMove, this.Current.Found, this.Current.Target,
                this.ReasonFor(pc, goal, now.AddSeconds(horizon + lead), margin), DodgeBlocker.MustNotMove, false));
            this.Current = SafeSpot.Stay;
            this.movement.Stop();
            return;
        }

        // steering is opt-in and only meaningful with a real controller installed
        var steering = this.config.AutoDodgeEnabled && this.Current.NeedToMove && this.Current.Found;

        // Stunned, asleep, bound or petrified: the game ignores movement input, so a steer is a lie the
        // whole way down -- the mover reports it is driving and the character does not move (Orthos,
        // 2026-09-06). Say so instead, and leave the facing alone: a gaze still resolves while stunned.
        var incapacitated = steering && Incapacitation.Blocking(pc) != null;
        if (incapacitated)
            steering = false;

        // A hold says the character is out of position on purpose -- Daedalus walking a healer to a corpse
        // 30y from the boss for an eight second raise. Without it the two engines fight over one character:
        // Regain keeps steering back to uptime, the cast never gets its still seconds, and neither side is
        // wrong on its own terms because neither can see the other's intent. Nothing but saying so settles
        // it, which is what this is.
        //
        // Only uptime movement yields. If the ground underfoot is about to kill them the hold is ignored
        // and the dodge runs: a raiser who dies mid-cast has raised nobody, so a hold that could pin someone
        // in an AOE would cost more than the stall it fixes. Narrower than BossmodReborn's AI.PauseMovement
        // on purpose -- that one stops everything, and Daedalus's own note on it admits the risk.
        var held = false;
        if (steering && this.HoldActive && !this.hints.InImminentDanger(pc.Position, now.AddSeconds(horizon + lead), margin))
        {
            steering = false;
            held = true;
        }

        // BossmodReborn stops all movement for the last half second of a gaze: a step turns the character
        // along its walk, and no facing survives that. Ahead of the Competition, 2026-09-05: a half-second
        // dodge walked the character from 169 to 25 degrees off a Holy Sphere, and the gaze landed.
        var gazeHold = false;
        if (steering && this.SecondsUntilGaze <= GazeHoldSeconds)
        {
            steering = false;
            gazeHold = true;
        }

        // Movement written below the input layer cannot interrupt a hardcast: the character simply does
        // not move (a Sage stood through an eight second raise in a seven second puddle). If the ground is
        // lethal the cast is cancelled, as BossmodReborn's mechanic AI does; if it is not, the cast is left
        // alone and the dodge waits -- an uptime walk is never worth a raise.
        var casting = false;
        if (steering && pc.CastInfo != null && this.movement is not NullMovementController)
        {
            if (this.hints.InImminentDanger(pc.Position, now.AddSeconds(horizon + lead), margin))
                GameData.CancelCast();
            else
            {
                steering = false;
                casting = true;
            }
        }

        // A hard cast cannot begin while we are walking the character, so the budget for one is nothing.
        // AIHints.MaxCastTime cannot know this: it prices the ground, and the ground is fine here --
        // Regain returns to uptime with no danger anywhere, so the geometry says float.MaxValue while the
        // character is being steered. A consumer that trusts that number starts an eight second raise that
        // can never begin: the game refuses it for "moving", nothing says why, and the raise silently never
        // happens for the whole fight. Reported as 0 for the same reason a stand-still punisher is -- the
        // budget really is zero -- and only when a real controller is installed, since the null one
        // computes the dodge for the radar without ever moving anyone.
        if (steering && this.movement is not NullMovementController)
            this.MaxCastTime = 0f;

        if (steering)
            // Steer is where to head this frame (used when we drive directly); Route is the whole path,
            // for a navmesh follower that walks what it is given rather than being steered.
            this.movement.MoveTo(this.Current.Steer, this.Current.Route);
        else
            this.movement.Stop();

        var blocker = steering ? DodgeBlocker.None
            : !this.config.AutoDodgeEnabled ? DodgeBlocker.AutoDodgeOff
            : held ? DodgeBlocker.Hold
            : gazeHold ? DodgeBlocker.Gaze
            : casting ? DodgeBlocker.Casting
            : incapacitated ? DodgeBlocker.Incapacitated
            : this.movement is NullMovementController or MovementController { HookInstalled: false, UsingNavmesh: false } ? DodgeBlocker.NoController
            : DodgeBlocker.None;
        this.Publish(this.Decide(this.Current.NeedToMove, this.Current.Found, this.Current.Target,
            this.ReasonFor(pc, goal, now.AddSeconds(horizon + lead), margin), blocker, steering));
    }

    private DodgeDecision published;

    /// <summary>How long before a gaze resolves the dodge stops walking; BossmodReborn's AI uses half a second.</summary>
    private const float GazeHoldSeconds = 0.6f;

    // the facing half of the decision rides along: whether a gaze was up and whether Minerva answered it
    private DodgeDecision Decide(bool needToMove, bool found, WPos target, DodgeReason reason, DodgeBlocker blocker, bool steering)
        => new(needToMove, found, target, reason, blocker, steering)
        {
            GazeUp = this.FacingConstrained,
            Turning = this.SafeFacing != null && this.config.AutoFaceGazes,
            Mover = this.movement.Mode,
            MoverBusy = this.movement.Busy,
        };

    /// <summary>
    /// Put the decision into the op stream when it changes, so a recording carries what the dodge chose
    /// beside what the world did -- and "why did I eat that" is answered from the log, not from memory.
    /// Executing it on the live world sets <see cref="WorldState.LastDodge"/> and fires Modified, which is
    /// all the recorder listens to; nothing else reads it live.
    /// </summary>
    private void Publish(DodgeDecision decision)
    {
        if (decision == this.published)
            return;
        this.published = decision;
        this.world.Execute(new OpDodgeDecision(decision));
    }

    /// <summary>The one-word reason for this frame's move, derived the way the solver ranks things.</summary>
    private DodgeReason ReasonFor(Actor pc, UptimeGoal? goal, DateTime deadline, float margin)
    {
        if (!this.Current.NeedToMove)
            return DodgeReason.None;
        if (!this.Current.Found)
            return DodgeReason.NoSafeSpot;
        if (this.hints.InImminentDanger(pc.Position, deadline, margin))
            return DodgeReason.Danger;
        if (goal is { } g)
        {
            if ((pc.Position - g.Target).Length() > g.Range)
                return DodgeReason.Uptime;
            if (!g.Satisfied(pc.Position))
                return DodgeReason.Positional;
        }
        return DodgeReason.Uptime;
    }

    /// <summary>
    /// Instance IDs the active module wants attacked ahead of everything else, best first.
    ///
    /// <para><b>Only actual opinions.</b> Every hostile is seeded at priority 0, so returning the whole
    /// list would hand a consumer an ordering Minerva never meant — and it cannot tell an opinion from a
    /// default. Only enemies a module explicitly raised appear here; when the fight has nothing to say
    /// this is empty, which is the signal to use your own targeting strategy untouched.</para>
    ///
    /// <para>This is the piece a rotation cannot derive: which add to burn before the boss, which two to
    /// bring down together. The module knows because the fight was authored; the rotation sees a list of
    /// hostiles and their HP.</para>
    /// </summary>
    public ulong[] PriorityTargets
    {
        get
        {
            if (!this.HasSolution)
                return [];
            var live = new List<(ulong id, int prio)>();
            foreach (var e in this.hints.PotentialTargets)
                if (e.Priority > 0)
                    live.Add((e.Actor.InstanceID, e.Priority));
            live.Sort(static (a, b) => b.prio.CompareTo(a.prio));
            var result = new ulong[live.Count];
            for (var i = 0; i < live.Count; ++i)
                result[i] = live[i].id;
            return result;
        }
    }

    /// <summary>
    /// Instance IDs worth attacking, but only once nothing better is left — a module saying "not this one
    /// yet" without forbidding it. Distinct from <see cref="ForbiddenTargets"/>: hitting these is legal,
    /// merely wasteful.
    /// </summary>
    public ulong[] DeprioritizedTargets
    {
        get
        {
            if (!this.HasSolution)
                return [];
            var ids = new List<ulong>();
            foreach (var e in this.hints.PotentialTargets)
                if (e.Priority < 0 && e.Priority > AIHints.Enemy.PriorityInvincible)
                    ids.Add(e.Actor.InstanceID);
            return [.. ids];
        }
    }

    /// <summary>Instance IDs the module says must NOT be attacked — invincible, or forbidden outright.</summary>
    public ulong[] ForbiddenTargets
    {
        get
        {
            if (!this.HasSolution)
                return [];
            var bad = new List<ulong>();
            foreach (var e in this.hints.PotentialTargets)
                if (e.Priority <= AIHints.Enemy.PriorityInvincible)
                    bad.Add(e.Actor.InstanceID);
            return [.. bad];
        }
    }

    /// <summary>
    /// Party slots carrying something the fight wants cleansed. Empty when nothing does.
    ///
    /// <para>Fight-authored: a module marks the slot because it knows this debuff matters here, which a
    /// rotation scanning status ids cannot tell from the dozens that do not.</para>
    /// </summary>
    public int[] CleanseTargets
    {
        get
        {
            if (!this.HasSolution)
                return [];
            var slots = new List<int>();
            for (var i = 0; i < PartyState.MaxSlots; ++i)
                if (this.hints.ShouldCleanse[i])
                    slots.Add(i);
            return [.. slots];
        }
    }

    /// <summary>
    /// Whether a cleanse has already been fired at this actor but has not yet landed.
    ///
    /// <para>Pairs with <see cref="CleanseTargets"/>: without it a boxed party throws several Esunas at one
    /// debuff, because the status is still on the bar until the first one resolves.</para>
    /// </summary>
    public bool CleansePending(ulong instanceId)
        => this.world.Actors.Find(instanceId)?.PendingDispels.Count > 0;

    /// <summary>Instance ID of the one thing the fight wants attacked, or 0. Outranks priorities.</summary>
    public ulong ForcedTargetId => this.HasSolution && this.hints.ForcedTarget is { IsDeadOrDestroyed: false } f ? f.InstanceID : 0uL;

    /// <summary>Instance IDs whose current cast the module wants interrupted.</summary>
    public ulong[] TargetsToInterrupt => this.Collect(static e => e.ShouldBeInterrupted);

    /// <summary>Instance IDs the module wants stunned.</summary>
    public ulong[] TargetsToStun => this.Collect(static e => e.ShouldBeStunned);

    private ulong[] Collect(Func<AIHints.Enemy, bool> pick)
    {
        if (!this.HasSolution)
            return [];
        var ids = new List<ulong>();
        foreach (var e in this.hints.PotentialTargets)
            if (pick(e))
                ids.Add(e.Actor.InstanceID);
        return [.. ids];
    }

    /// <summary>
    /// Instance ID of the tank who must hand the boss over, or 0 when no swap is due.
    ///
    /// <para>Whoever this is should drop aggro (Shirk, or simply stop attacking); any other tank should
    /// provoke. BossmodReborn only prints "Provoke!"/"Pass aggro!" at whichever character is being read,
    /// which cannot be acted on when one plugin is driving both tanks — the identity is the decision.</para>
    /// </summary>
    public ulong TankSwapCurrentTank => this.HasSolution ? this.hints.TankSwapCurrentTank : 0uL;

    /// <summary>Seconds until the swap must be done, or NaN when no swap is due / the timing is unknown.</summary>
    public float SecondsUntilTankSwap
    {
        get
        {
            if (!this.HasSolution || this.hints.TankSwapCurrentTank == 0uL || this.hints.TankSwapBy == default)
                return float.NaN;
            return MathF.Max((float)(this.hints.TankSwapBy - this.world.CurrentTime).TotalSeconds, 0f);
        }
    }

    /// <summary>
    /// Net HP change already announced against an actor but not yet drawn — negative for inbound damage.
    ///
    /// <para>The server confirms a resolved action before the client applies it. Inside that window the
    /// hit is a fact: who, and for how much. A healer reacting after the HP bar moves is reacting after
    /// the information was available, which on a raidwide is the difference between a shield landing and
    /// a shield being wasted.</para>
    ///
    /// <para>0 when the actor is unknown or nothing is inbound — those are indistinguishable, and
    /// deliberately so: both mean "no reason to act".</para>
    /// </summary>
    public int PendingHPDifference(ulong instanceId)
        => this.world.Actors.Find(instanceId)?.PendingHPDifference ?? 0;

    /// <summary>An actor's HP once everything announced has landed. Can go below zero, which is how
    /// "this hit is lethal" is answerable before the hit is drawn. 0 when the actor is unknown.</summary>
    public int PendingHPRaw(ulong instanceId)
        => this.world.Actors.Find(instanceId)?.PendingHPRaw ?? 0;

    /// <summary>Status ids announced against an actor but not applied yet.</summary>
    public uint[] PendingStatuses(ulong instanceId)
    {
        var a = this.world.Actors.Find(instanceId);
        if (a == null || a.PendingStatuses.Count == 0)
            return [];
        var ids = new uint[a.PendingStatuses.Count];
        for (var i = 0; i < ids.Length; ++i)
            ids[i] = a.PendingStatuses[i].StatusId;
        return ids;
    }

    /// <summary>
    /// Every party member's HP after announced effects land, indexed by party slot; <c>int.MinValue</c>
    /// for an empty or unresolvable slot.
    ///
    /// <para>Party-shaped because that is the question a healer actually asks — "who is about to be low"
    /// is one call rather than eight, and the answer has to be comparable across members to be useful.
    /// Slots are the game's party-list order; the local player is not reliably slot 0.</para>
    /// </summary>
    public int[] PartyPendingHP()
    {
        var res = new int[PartyState.MaxSlots];
        for (var i = 0; i < PartyState.MaxSlots; ++i)
        {
            var a = this.world.Party.Actor(i);
            res[i] = a != null ? a.PendingHPRaw : int.MinValue;
        }
        return res;
    }

    /// <summary>Seconds until the soonest predicted damage of <paramref name="type"/>, or NaN if none is
    /// known to be coming.</summary>
    private float SecondsUntilDamage(AIHints.PredictedDamageType type)
    {
        if (!this.HasSolution)
            return float.NaN;
        var now = this.world.CurrentTime;
        var soonest = DateTime.MaxValue;
        foreach (var (_, activation, t) in this.hints.PredictedDamage)
            if (t == type && activation < soonest)
                soonest = activation;
        return soonest == DateTime.MaxValue ? float.NaN : MathF.Max((float)(soonest - now).TotalSeconds, 0f);
    }

    /// <summary>Seconds until the next raidwide, or NaN. Mitigation timing.</summary>
    public float SecondsUntilRaidwide => this.SecondsUntilDamage(AIHints.PredictedDamageType.Raidwide);

    /// <summary>Seconds until the next tankbuster, or NaN.</summary>
    public float SecondsUntilTankbuster => this.SecondsUntilDamage(AIHints.PredictedDamageType.Tankbuster);

    /// <summary>Seconds until the next shared/stack damage, or NaN.</summary>
    public float SecondsUntilSharedDamage => this.SecondsUntilDamage(AIHints.PredictedDamageType.Shared);

    /// <summary>
    /// Party-slot bitmask of who the next tankbuster is aimed at, or 0 when none is known.
    ///
    /// <para>This is the half a tank swap is actually decided from. A timer says "mitigate"; the mask says
    /// "it is aimed at the main tank again, so the off-tank should provoke". Slots are Minerva's party
    /// slots, which are the game's party-list order — note that the local player is NOT reliably slot 0.</para>
    /// </summary>
    public uint NextTankbusterTargets
    {
        get
        {
            if (!this.HasSolution)
                return 0u;
            var soonest = DateTime.MaxValue;
            BitMask mask = default;
            foreach (var (players, activation, t) in this.hints.PredictedDamage)
                if (t == AIHints.PredictedDamageType.Tankbuster && activation < soonest)
                {
                    soonest = activation;
                    mask = players;
                }
            return (uint)mask.Raw;
        }
    }

    /// <summary>
    /// Is that world position clear of everything the active module currently forbids? Published over IPC
    /// for consumers deciding where to stand — a revival routine picking a spot to raise from.
    /// <para>Answers about now, not about the length of a cast; pair it with <see cref="MaxCastTime"/>,
    /// which is the one that knows how long the spot stays good.</para>
    /// </summary>
    public bool IsPositionSafe(System.Numerics.Vector3 to)
        => !this.HasSolution || this.hints.IsPositionSafe(new WPos(to.X, to.Z));

    /// <summary>As <see cref="IsPositionSafe"/>, and the straight line there does not leave the arena.</summary>
    public bool IsDashSafe(System.Numerics.Vector3 from, System.Numerics.Vector3 to)
        => !this.HasSolution || this.hints.IsDashSafe(new WPos(from.X, from.Z), new WPos(to.X, to.Z));

    /// <summary>Seconds from now, floored at zero. NaN means "no such thing is coming", which a consumer
    /// tests with a single <c>float.IsNaN</c> rather than agreeing on a sentinel.</summary>
    private static float Countdown(DateTime? at, DateTime now)
        => at is { } t ? MathF.Max((float)(t - now).TotalSeconds, 0f) : float.NaN;

    /// <summary>
    /// What to keep uptime on: whatever the fight says to attack if it says anything, then the boss when a
    /// module names one, otherwise whatever the player has targeted. The last case is the whole of
    /// unscripted content, where the dodge previously had no reason to stay near anything and would
    /// happily leave the pull to sidestep a puddle.
    ///
    /// <para><b>Why the forced target comes first.</b> Some fights are not won by hitting the boss. Tiny
    /// Terror wants a particular arcane sphere killed; the module knows which one and says so on the radar
    /// in words. Before this, that never reached the movement system: <see cref="AIHints.ForcedTarget"/>
    /// was written by several modules and read by nothing, so auto-move kept walking back to the boss and
    /// sat there reporting "no imminent danger" while the objective went untouched. Uptime means uptime on
    /// the right thing.</para>
    ///
    /// <para>A dead or despawned forced target is ignored rather than honoured, so a module that forgets to
    /// clear one cannot strand the player next to a corpse.</para>
    /// </summary>
    private Actor? UptimeTarget(ModuleBase? module, Actor pc)
    {
        var target = this.hints.ForcedTarget is { IsDeadOrDestroyed: false } forced ? forced
            : this.PrioritisedTarget(pc)
            ?? (module?.PrimaryActor is { IsDeadOrDestroyed: false } boss && !this.Forbidden(boss) ? boss
            : this.world.Actors.Find(pc.TargetID) is { IsDeadOrDestroyed: false, Type: ActorType.Enemy, IsAlly: false } t ? t
            : null);
        return target != null && InTheFight(module, pc, target) ? target : null;
    }

    /// <summary>
    /// The enemy the module's priorities say to be on: among the highest-priority legal ones, the player's
    /// own target if it is one (Daedalus applies the same list), else the nearest. Null when nothing is
    /// raised above the default, so an ordinary boss fight still keys on the primary actor.
    /// <para>Alexander, 2026-09-06: Perfect Defense made the boss invincible and four adds priority 1;
    /// Daedalus switched to the adds, and the walk-back kept the character at the boss, so the adds got
    /// three Tomahawks in forty seconds.</para>
    /// </summary>
    private Actor? PrioritisedTarget(Actor pc)
    {
        var top = int.MinValue;
        foreach (var e in this.hints.PotentialTargets)
            if (e.Priority > AIHints.Enemy.PriorityInvincible && !e.Actor.IsDeadOrDestroyed)
                top = Math.Max(top, e.Priority);
        if (top <= 0)
            return null;
        Actor? best = null;
        var bestDist = float.MaxValue;
        foreach (var e in this.hints.PotentialTargets)
        {
            if (e.Priority != top || e.Actor.IsDeadOrDestroyed)
                continue;
            if (e.Actor.InstanceID == pc.TargetID)
                return e.Actor;
            var d = (e.Actor.Position - pc.Position).LengthSq();
            if (d < bestDist)
            {
                bestDist = d;
                best = e.Actor;
            }
        }
        return best;
    }

    /// <summary>The module says not to attack this one (invincible, or forbidden outright): no uptime to regain on it.</summary>
    private bool Forbidden(Actor a)
    {
        foreach (var e in this.hints.PotentialTargets)
            if (e.Actor == a)
                return e.Priority <= AIHints.Enemy.PriorityInvincible;
        return false;
    }

    /// <summary>
    /// How far the dodge will walk to regain uptime when nothing authored says where the fight is. A FATE
    /// that starts nearby puts its boss in the actor table long before it is on screen, and "regain uptime"
    /// on a boss ninety yalms away is a run across the map -- which is what happened. Travel is not the
    /// dodge's job: inside this range you are in the fight, beyond it you are not.
    /// </summary>
    private const float UptimeLeash = TrashHorizon;

    /// <summary>
    /// Uptime is only worth regaining when the character is already in the fight. With an authored arena
    /// that means standing inside it: a boss module activates the moment its boss is in the actor table,
    /// which in the open world is well outside the arena. Without one (trash, or an open-world FATE whose
    /// bound is the FATE ring, which can be a hundred yalms across) it means the target is within the leash.
    /// The danger dodge is untouched either way; this only decides whether to walk <i>towards</i> something.
    /// </summary>
    private static bool InTheFight(ModuleBase? module, Actor pc, Actor target)
    {
        if (module != null && module is not OpenWorldFate)
            return module.Bounds.Contains(module.Center, pc.Position);
        return (target.Position - pc.Position).LengthSq() <= UptimeLeash * UptimeLeash;
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
            this.floorProbeDistrusted = false;
            this.probeRejectStreak = 0;
            this.knownGround.Reset();
        }

        var playerY = pc.PosRot.Y;
        foreach (var a in this.world.Actors)
        {
            if (a.IsDeadOrDestroyed || a.Type is not (ActorType.Player or ActorType.Enemy or ActorType.Buddy))
                continue;
            this.footprint.Observe(a.Position, a.PosRot.Y - playerY);
            if (MathF.Abs(a.PosRot.Y - playerY) <= ArenaFootprint.SameFloorTolerance)
            {
                this.knownGround.Observe(a.Position);
                // somebody standing in a "hole" settles it: the probe was wrong there
                for (var i = this.knownVoids.Count - 1; i >= 0; --i)
                    if ((this.knownVoids[i] - a.Position).LengthSq() < VoidRadius * VoidRadius)
                        this.knownVoids.RemoveAt(i);
            }
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

        // Deep dungeons have walls, not ledges: nothing here for the probe to catch, and on 2026-09-06 it
        // held the dodge still inside a Nerve Gas cone instead (Orthos 21-30: "no floor along the path"
        // on every frame of a flat floor). A probe that has already refused every frame is off for the zone.
        if (this.floorProbeDistrusted || GameSync.GameData.IsDeepDungeon(this.world.CurrentCFCID))
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
            if (RouteHasFloor(from, spot))
                return spot;

            // instrumentation: the probe caught a ledge — a discrete, low-frequency event worth a line each
            Service.Log.Information($"Minerva floor probe: REJECTED dodge target {spot.Target} — no floor along the path (attempt {attempt + 1}/{MaxFloorRetries}); re-solving around the void.");

            // A ledge is found once and remembered; the next solve routes round it. A probe that refuses
            // frame after frame is misreading the zone (Orthos, 2026-09-06: the same target refused thirty
            // times a second for six seconds while the party walked all over it), and every refusal it
            // remembered is a fake hole. Stand down for the zone and forget them.
            this.probeRejectStreak = (now - this.probeLastReject).TotalSeconds <= 0.25 ? this.probeRejectStreak + 1 : 1;
            this.probeLastReject = now;
            if (this.probeRejectStreak >= 30)
            {
                Service.Log.Information($"Minerva floor probe: STOOD DOWN — {this.probeRejectStreak} refusals in a row on ground nobody fell from; misreading this zone, floor checks off until the next one.");
                this.floorProbeDistrusted = true;
                this.knownVoids.Clear();
                return spot;
            }
            this.RememberVoid(spot.Target);
            this.hints.TemporaryObstacles.Add(new SDCircle(spot.Target, VoidRadius));
            spot = ArenaPathfinder.Solve(this.hints, now, horizonSeconds: horizon, safetyMargin: margin, goal: goal,
                clearanceLead: Math.Clamp(this.config.AutoDodgeClearanceLead, 0f, 5f));
            if (!spot.NeedToMove || !spot.Found)
                return spot;
        }

        // three answers in a row over the void: holding beats walking off, even inside an AOE
        Service.Log.Information("Minerva floor probe: HELD — three floorless answers in a row; holding position rather than dodging off the edge.");
        return SafeSpot.Stay;
    }

    /// <summary>
    /// Is there floor along every leg the character will actually walk?
    ///
    /// <para>Probing the straight line to the destination was right while the mover was handed a single
    /// point. It stopped being right once the route is handed over in full: a path that bends around an
    /// AOE can cross a ledge the chord never touches, and can equally avoid one the chord runs straight
    /// over -- the first walks the character off the edge, the second refuses a destination that was fine.
    /// Falls back to the chord when no route was computed, which is what the direct-steering path walks.</para>
    /// </summary>
    private bool RouteHasFloor(Vector4 from, SafeSpot spot)
    {
        var at = new Vector3(from.X, from.Y, from.Z);
        if (spot.Route is not { Count: > 0 } route)
            return this.SegmentHasFloor(at, new Vector3(spot.Target.X, from.Y, spot.Target.Z));

        for (var i = 0; i < route.Count; ++i)
        {
            var next = new Vector3(route[i].X, from.Y, route[i].Z);
            if (!this.SegmentHasFloor(at, next))
                return false;
            at = next;
        }
        return true;
    }

    /// <summary>One leg of the walk, sampled every four yalms like <see cref="GameSync.GameData.PathHasFloor"/>,
    /// except that a sample on ground somebody has stood on is ground whatever the ray says.</summary>
    private bool SegmentHasFloor(Vector3 from, Vector3 to)
    {
        var dx = to.X - from.X;
        var dz = to.Z - from.Z;
        var dist = MathF.Sqrt((dx * dx) + (dz * dz));
        var steps = Math.Clamp((int)MathF.Ceiling(dist / 4f), 1, 8);
        for (var i = 1; i <= steps; ++i)
        {
            var t = (float)i / steps;
            var x = from.X + (dx * t);
            var z = from.Z + (dz * t);
            if (this.knownGround.Contains(new WPos(x, z)))
                continue;
            if (!GameSync.GameData.HasFloorAt(x, z, from.Y))
                return false;
        }
        return true;
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
        {
            // auto-attack re-faces the target every frame and would undo the turn before the next frame
            GameData.StopAutoAttack();
            this.movement.Face(facing);
        }
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

    /// <summary>
    /// Stand still for the next few seconds even though there is uptime to be regained -- a rotation saying
    /// "I walked this healer to a corpse on purpose, stop bringing them back".
    ///
    /// <para>Only uptime movement yields; danger still moves the character. See the call site in
    /// <c>Update</c> for why, and <see cref="MaxCastTime"/>, which stops reading 0 while a hold is honoured
    /// because standing out of uptime is no longer something we are about to undo.</para>
    ///
    /// <para>Expires on its own, like <see cref="RequestPositional"/>, and for the same reason: a caller
    /// that crashes or abandons the cast must not be able to park the character indefinitely. Re-assert
    /// each frame you still want it. A later call replaces the deadline rather than extending it, so a
    /// caller may also shorten its own hold by asking for less.</para>
    /// </summary>
    public void RequestHold(double seconds)
        => this.holdUntil = this.world.CurrentTime.AddSeconds(Math.Clamp(seconds, 0d, 30d));

    /// <summary>Whether a <see cref="RequestHold"/> is in force this frame.</summary>
    public bool HoldActive => this.holdUntil > this.world.CurrentTime;

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
        // a gaze is worth answering even when there is nothing on the ground to walk out of
        if (!this.config.AutoHintsForTrash || this.autoHints is not { } guess || (guess.Count == 0 && guess.GazeCount == 0))
            return false;

        this.hints.Clear();
        this.hints.PlayerPosition = pc.Position;

        // Prefer the arena we have watched people stand in over a window centred on the player. The window
        // is what walks you off a platform: stand on the edge and half of it is over the drop, so the far
        // side of an edge-hugging AOE reads as clear ground. An arena-centred box has no such far side.
        if (this.footprint.TryEstimate(pc.Position, out var center, out var bounds))
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
        this.autoHints.AddForbiddenDirections(this.hints, pc.Position);
        this.ApplyKnownVoids();
        return true;
    }
}
