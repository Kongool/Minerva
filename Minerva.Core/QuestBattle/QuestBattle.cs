namespace Minerva.QuestBattle;

/// <summary>A point a quest script walks through. <paramref name="Pathfind"/> false walks the straight line to it
/// instead of asking the navmesh, for the legs the mesh does not know (a jump down, a ramp it never built).</summary>
public record struct Waypoint(Vector3 Position, bool Pathfind = true);

public enum NavigationStrategy
{
    /// <summary>Keep walking even in combat (an escort, a run past the mobs).</summary>
    Continue,
    /// <summary>Drop the walk on combat and do not resume it.</summary>
    Stop,
    /// <summary>Drop the walk on combat and pick it up again from where the character stands once it ends.</summary>
    Pause,
}

/// <summary>
/// The game's condition flags a quest script reacts to, by Dalamud's name for them. Minerva.Core does not reference
/// Dalamud, so the plugin forwards each change by name (<see cref="QuestBattle.OnConditionChange"/>); a flag not listed
/// here is simply never forwarded. Add a name when a script needs one.
/// </summary>
public enum ConditionFlag
{
    BetweenAreas,
    BeingMoved,
    Jumping61,
}

/// <summary>The navmesh, for a quest script's walk between waypoints. Installed by the plugin (Ariadne, else
/// vnavmesh); null in tests and offline, where scripts still advance on events but walk nowhere.</summary>
public interface IQuestNavigation
{
    bool IsReady { get; }

    /// <summary>A walkable path from <paramref name="from"/> to <paramref name="to"/>, or null when none can be asked.</summary>
    Task<List<Vector3>>? Pathfind(Vector3 from, Vector3 to);
}

/// <summary>
/// One step of a quest battle: where to walk, what to click, what to fight, and what event ends it. Ported from
/// BossmodReborn's <c>QuestObjective</c> (BSD-3; see THIRD-PARTY-NOTICES.txt) with its builder API intact, so the
/// duty scripts port as written.
/// </summary>
public sealed class QuestObjective(WorldState ws)
{
    public readonly WorldState World = ws;
    public string Name { get; private set; } = "";
    public readonly List<Waypoint> Connections = [];
    public NavigationStrategy NavigationStrategy = NavigationStrategy.Pause;

    public string DisplayName => this.Name.Length > 0 ? this.Name
        : this.Connections.Count > 0 ? Utils.Vec3String(this.Connections[^1].Position)
        : "<none>";

    public bool ForceStopNavigation;
    public bool Completed;

    public Action<Actor, AIHints>? AddAIHints;
    public Action<Actor>? OnModelStateChanged;
    public Action<Actor, ActorStatus>? OnStatusGain;
    public Action<Actor, ActorStatus>? OnStatusLose;
    public Action<Actor>? OnActorEventStateChanged;
    public Action<Actor, ushort>? OnEventObjectStateChanged;
    public Action<Actor, ushort, ushort>? OnEventObjectAnimation;
    public Action<Actor>? OnActorCreated;
    public Action<Actor>? OnActorDestroyed;
    public Action<Actor>? OnActorCombatChanged;
    public Action<Actor>? OnActorKilled;
    public Action<Actor>? OnActorTargetableChanged;
    public Action<Actor, ActorCastEvent>? OnEventCast;
    public Action<WorldState.OpMapEffect>? OnMapEffect;
    public Action<WorldState.OpDirectorUpdate>? OnDirectorUpdate;
    public Action<ClientState.DutyAction[]>? OnDutyActionsChange;
    public Action<ConditionFlag, bool>? OnConditionChange;
    public Action? OnNavigationComplete;
    public Action? Update;

    public QuestObjective Named(string name)
    {
        this.Name = name;
        return this;
    }

    public QuestObjective WithConnection(Vector3 conn) => this.WithConnection(new Waypoint(conn));

    public QuestObjective WithConnection(Waypoint conn)
    {
        this.Connections.Add(conn);
        return this;
    }

    public QuestObjective WithConnections(params Vector3[] connections)
    {
        foreach (var c in connections)
            this.Connections.Add(new Waypoint(c));
        return this;
    }

    public QuestObjective WithConnections(params Waypoint[] connections)
    {
        this.Connections.AddRange(connections);
        return this;
    }

    public QuestObjective With(Action<QuestObjective> act)
    {
        act(this);
        return this;
    }

    public QuestObjective MoveHint(WPos destination, float weight = 0.5f)
    {
        this.AddAIHints += (player, hints) => hints.GoalZones.Add(p => p.InCircle(destination, 2f) ? weight : 0f);
        return this;
    }

    public QuestObjective MoveHint(Vector3 destination) => this.MoveHint(new WPos(destination.X, destination.Z));

    public QuestObjective PauseForCombat(bool pause)
    {
        this.NavigationStrategy = pause ? NavigationStrategy.Pause : NavigationStrategy.Continue;
        return this;
    }

    public QuestObjective StopOnCombat()
    {
        this.NavigationStrategy = NavigationStrategy.Stop;
        return this;
    }

    public QuestObjective CompleteOnTargetable(uint oid, bool targetable)
    {
        this.OnActorTargetableChanged += act => this.CompleteIf(act.OID == oid && act.IsTargetable == targetable);
        return this;
    }

    public QuestObjective CompleteOnCreated(uint oid)
    {
        this.OnActorCreated += act => this.CompleteIf(act.OID == oid);
        return this;
    }

    public QuestObjective CompleteOnKilled(uint oid, int required = 1)
    {
        var killed = 0;
        this.OnActorKilled += act =>
        {
            if (act.OID == oid && ++killed >= required)
                this.Completed = true;
        };
        return this;
    }

    public QuestObjective CompleteOnDestroyed(uint oid)
    {
        this.OnActorDestroyed += act => this.CompleteIf(act.OID == oid);
        return this;
    }

    public QuestObjective CompleteOnState7(uint oid)
    {
        this.OnActorEventStateChanged += act => this.CompleteIf(act.OID == oid && act.EventState == 7);
        return this;
    }

    public QuestObjective ThenWait(float seconds)
    {
        var until = DateTime.MaxValue;
        this.OnNavigationComplete += () => until = this.World.FutureTime(seconds);
        this.Update += () => this.CompleteIf(this.World.CurrentTime > until);
        return this;
    }

    public QuestObjective Hints(Action<Actor, AIHints> addHints)
    {
        this.AddAIHints += addHints;
        return this;
    }

    public QuestObjective WithInteract<OID>(OID targetOid, bool allowInCombat = false) where OID : Enum
        => this.WithInteract((uint)(object)targetOid, allowInCombat);

    public QuestObjective WithInteract(uint targetOid, bool allowInCombat = false)
    {
        this.AddAIHints += (player, hints) =>
        {
            if (!player.InCombat || allowInCombat)
                hints.InteractWith(this.World, targetOid);
        };
        return this;
    }

    public QuestObjective CompleteAtDestination()
    {
        this.OnNavigationComplete += () => this.Completed = true;
        return this;
    }

    public static QuestObjective Combat(WorldState ws, params Vector3[] connections)
        => new QuestObjective(ws).WithConnections(connections)
            .With(obj => obj.OnActorCombatChanged += act => obj.CompleteIf(act.OID == 0 && !act.InCombat));

    public static QuestObjective StandardInteract(WorldState ws, uint oid, params Vector3[] connections)
        => new QuestObjective(ws).WithConnections(connections).WithInteract(oid).CompleteOnState7(oid);

    public override string ToString() => $"{this.Name}{(this.Connections.Count == 0 ? "" : Utils.Vec3String(this.Connections[^1].Position))}";

    public void CompleteIf(bool c) => this.Completed |= c;
}

/// <summary>
/// A solo duty played start to finish: the walk between rooms, the things to click, the mobs to clear, the character
/// to play. Ported from BossmodReborn's <c>QuestBattle</c> (BSD-3; see THIRD-PARTY-NOTICES.txt), which is how a solo
/// duty Odysseus hands to the boss engine gets finished. Until 2026-10-01 Minerva had none of the 97 scripts, so a
/// duty handed to it was fought where the character stood and walked nowhere.
///
/// <para>What changed in the port:</para>
/// <list type="bullet">
/// <item>The walk is not <c>ForcedMovement</c>. The script publishes the path still ahead
/// (<see cref="AIHints.QuestTravel"/>), and the AI hands it to the navmesh follower whenever nothing is dangerous:
/// a dodge still comes first, and casting is never interrupted for a walk.</item>
/// <item>No dashes toward waypoints. BossmodReborn pressed the job's gap closer on long legs; Minerva presses no
/// buttons.</item>
/// <item>Nothing runs while a boss module is in charge, as in BossmodReborn, where a zone module's hints are only
/// asked for without an active boss module. Objectives still advance on events meanwhile.</item>
/// <item>Pathfinding comes from <see cref="Navigation"/>, which the plugin installs; offline it is null and the
/// script advances without walking.</item>
/// </list>
/// </summary>
public abstract class QuestBattle : ZoneModule
{
    /// <summary>The navmesh used to walk between waypoints; set once by the plugin.</summary>
    public static IQuestNavigation? Navigation;

    /// <summary>Where objective changes are logged; set once by the plugin.</summary>
    public static Action<string>? LogSink;

    /// <summary>A waypoint is reached when the character is this close in X/Z.</summary>
    public const float Tolerance = 1f;

    /// <summary>The last waypoint of an objective's path, where a click or a wait follows, wants a closer arrival.</summary>
    public const float FinalTolerance = 0.5f;

    /// <summary>How far around the character something to fight counts as "a target", BossmodReborn's load range for
    /// overworld duties; Minerva's trash horizon.</summary>
    public const float TargetRange = 30f;

    public List<QuestObjective> Objectives;
    public int CurrentObjectiveIndex { get; private set; }
    public QuestObjective? CurrentObjective => this.CurrentObjectiveIndex >= 0 && this.CurrentObjectiveIndex < this.Objectives.Count ? this.Objectives[this.CurrentObjectiveIndex] : null;

    private readonly record struct NavigationWaypoint(Vector3 Position, bool SpecifiedInPath);

    private readonly List<IDisposable> subscriptions = [];
    private Task<List<NavigationWaypoint>>? pathfindTask;
    private List<NavigationWaypoint> currentWaypoints = [];
    private int navigationProgress;
    private bool combatFlag;
    private bool playerLoaded;
    private readonly uint[] dutyActionIds = new uint[ClientState.NumDutyActions];

    /// <summary>Stops the walk without losing the objective, for the debug window.</summary>
    public bool Paused;

    /// <summary>Waypoints still ahead on this objective; empty when standing still.</summary>
    public IReadOnlyList<Vector3> RemainingPath => this.currentWaypoints.ConvertAll(w => w.Position);

    protected static Vector3 V3(float x, float y, float z) => new(x, y, z);

    private static void Log(string msg) => LogSink?.Invoke($"[QuestBattle] {msg}");

    protected QuestBattle(WorldState ws) : base(ws)
    {
#pragma warning disable CA2214 // the scripts build their objectives from the world alone, as in BossmodReborn
        this.Objectives = this.DefineObjectives(ws);
#pragma warning restore CA2214

        var actors = ws.Actors;
        this.subscriptions.Add(actors.EventStateChanged.Subscribe((act, _) => this.CurrentObjective?.OnActorEventStateChanged?.Invoke(act)));
        this.subscriptions.Add(actors.CastEvent.Subscribe((act, ev) => this.CurrentObjective?.OnEventCast?.Invoke(act, ev)));
        this.subscriptions.Add(actors.StatusLose.Subscribe((act, ix) => this.CurrentObjective?.OnStatusLose?.Invoke(act, act.Statuses[ix])));
        this.subscriptions.Add(actors.StatusGain.Subscribe((act, ix) => this.CurrentObjective?.OnStatusGain?.Invoke(act, act.Statuses[ix])));
        this.subscriptions.Add(actors.ModelStateChanged.Subscribe((act, _) => this.CurrentObjective?.OnModelStateChanged?.Invoke(act)));
        this.subscriptions.Add(actors.Added.Subscribe(act => this.CurrentObjective?.OnActorCreated?.Invoke(act)));
        this.subscriptions.Add(actors.Removed.Subscribe(act => this.CurrentObjective?.OnActorDestroyed?.Invoke(act)));
        this.subscriptions.Add(actors.InCombatChanged.Subscribe(act => this.CurrentObjective?.OnActorCombatChanged?.Invoke(act)));
        this.subscriptions.Add(actors.IsDeadChanged.Subscribe(act =>
        {
            if (act.IsDead)
                this.CurrentObjective?.OnActorKilled?.Invoke(act);
        }));
        this.subscriptions.Add(actors.EStateChanged.Subscribe((act, state) => this.CurrentObjective?.OnEventObjectStateChanged?.Invoke(act, state)));
        // the sync packs the two animation parameters into one: first << 16 | second
        this.subscriptions.Add(actors.EAnimChanged.Subscribe((act, packed) => this.CurrentObjective?.OnEventObjectAnimation?.Invoke(act, (ushort)(packed >> 16), (ushort)packed)));
        this.subscriptions.Add(actors.IsTargetableChanged.Subscribe(act => this.CurrentObjective?.OnActorTargetableChanged?.Invoke(act)));
        this.subscriptions.Add(ws.DirectorUpdate.Subscribe(op => this.CurrentObjective?.OnDirectorUpdate?.Invoke(op)));
        this.subscriptions.Add(ws.MapEffect.Subscribe(op => this.CurrentObjective?.OnMapEffect?.Invoke(op)));
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var s in this.subscriptions)
            s.Dispose();
        this.subscriptions.Clear();
        base.Dispose(disposing);
    }

    public virtual List<QuestObjective> DefineObjectives(WorldState ws) => [];

    public virtual void AddQuestAIHints(Actor player, AIHints hints) { }

    /// <summary>A game condition flag changed; forwarded by the plugin.</summary>
    public void OnConditionChange(ConditionFlag flag, bool value) => this.CurrentObjective?.OnConditionChange?.Invoke(flag, value);

    public override void Update()
    {
        if (!this.playerLoaded && this.World.Party.Player() is { } player)
        {
            this.playerLoaded = true;
            if (this.CurrentObjective is { } obj)
                this.TryPathfind(player.PosRot.XYZ(), obj.Connections);
        }

        if (this.pathfindTask is { IsCompletedSuccessfully: true } done)
        {
            this.currentWaypoints = done.Result;
            this.pathfindTask = null;
        }
        else if (this.pathfindTask is { IsFaulted: true } or { IsCanceled: true })
        {
            Log($"pathfind failed for {this.CurrentObjective}");
            this.pathfindTask = null;
        }

        // the duty's actions are polled by the sync, not sent as an op, so a change is noticed here
        var changed = false;
        var duty = this.World.Client.DutyActions;
        for (var i = 0; i < duty.Length; ++i)
        {
            if (this.dutyActionIds[i] != duty[i].Action.ID)
            {
                this.dutyActionIds[i] = duty[i].Action.ID;
                changed = true;
            }
        }
        if (changed)
            this.CurrentObjective?.OnDutyActionsChange?.Invoke(duty);
    }

    public override void CalculateAIHints(int playerSlot, Actor player, AIHints hints)
    {
        hints.QuestDriven = true;
        var restartPathfind = false;

        var flag = HaveTarget(player, hints);
        if (flag != this.combatFlag)
        {
            this.combatFlag = flag;
            if (flag)
            {
                if (this.CurrentObjective is { NavigationStrategy: NavigationStrategy.Stop or NavigationStrategy.Pause })
                    this.currentWaypoints.Clear();
            }
            else if (this.CurrentObjective is { NavigationStrategy: NavigationStrategy.Pause })
            {
                Log("out of combat, walking on");
                restartPathfind = true;
            }
        }

        if (this.CurrentObjective is { } cur)
        {
            cur.Update?.Invoke();
            if (cur.Completed)
            {
                ++this.CurrentObjectiveIndex;
                restartPathfind |= this.OnObjectiveChanged();
            }
        }

        if (this.CurrentObjective is { } obj)
        {
            obj.AddAIHints?.Invoke(player, hints);

            if (restartPathfind)
                this.TryPathfind(player.PosRot.XYZ(), obj.Connections[Math.Min(this.navigationProgress, obj.Connections.Count)..]);

            if (!this.Paused && !obj.ForceStopNavigation)
                this.MoveNext(player, obj, hints);
        }

        AllyHeals.Request(this.World, player, hints);
        this.AddQuestAIHints(player, hints);
    }

    /// <summary>
    /// Is there something to fight: the character is in combat, or an enemy worth attacking is within reach.
    /// BossmodReborn's test, asked before the objective's own hints raise anything, so "worth attacking" means
    /// already in a fight (Minerva seeds those at 0, everything else below).
    /// </summary>
    public static bool HaveTarget(Actor player, AIHints hints)
    {
        if (player.InCombat)
            return true;
        foreach (var e in hints.PotentialTargets)
            if (e.Priority >= 0 && !e.Actor.IsDeadOrDestroyed && (e.Actor.Position - player.Position).LengthSq() <= TargetRange * TargetRange)
                return true;
        return false;
    }

    /// <summary>True when the walk should restart for the new objective.</summary>
    private bool OnObjectiveChanged()
    {
        var obj = this.CurrentObjective;
        this.navigationProgress = 0;
        Log($"next objective: {(obj == null ? "<done>" : obj.DisplayName)} ({this.CurrentObjectiveIndex + 1}/{this.Objectives.Count})");
        this.currentWaypoints.Clear();
        return obj != null && (!this.combatFlag || obj.NavigationStrategy == NavigationStrategy.Continue);
    }

    private void TryPathfind(Vector3 start, List<Waypoint> connections)
    {
        if (connections.Count == 0)
            return;
        if (Navigation == null)
        {
            // offline: the objective's points as given, straight lines between them
            var straight = new List<NavigationWaypoint>(connections.Count);
            foreach (var c in connections)
                straight.Add(new NavigationWaypoint(c.Position, true));
            this.currentWaypoints = straight;
            return;
        }

        List<Waypoint> points = [new Waypoint(start), .. connections];
        this.pathfindTask = Task.Run(() => PathfindAll(Navigation, points));
    }

    private static async Task<List<NavigationWaypoint>> PathfindAll(IQuestNavigation nav, List<Waypoint> points)
    {
        while (!nav.IsReady)
        {
            Log("navmesh is not ready - waiting");
            await Task.Delay(500).ConfigureAwait(false);
        }

        var result = new List<NavigationWaypoint>();
        for (var i = 1; i < points.Count; ++i)
        {
            var from = points[i - 1].Position;
            var to = points[i];
            if (!to.Pathfind)
            {
                result.Add(new NavigationWaypoint(to.Position, true));
                continue;
            }

            var task = nav.Pathfind(from, to.Position);
            var path = task == null ? null : await task.ConfigureAwait(false);
            if (path == null || path.Count == 0)
            {
                // no route: walk the straight line rather than stop dead, and say so
                Log($"no route {Utils.Vec3String(from)} -> {Utils.Vec3String(to.Position)}; walking straight");
                result.Add(new NavigationWaypoint(to.Position, true));
                continue;
            }

            for (var j = 0; j < path.Count - 1; ++j)
                result.Add(new NavigationWaypoint(path[j], false));
            // the mesh's end point stands in for the one the script named, as in BossmodReborn
            result.Add(new NavigationWaypoint(path[^1], true));
        }
        return result;
    }

    private void MoveNext(Actor player, QuestObjective objective, AIHints hints)
    {
        while (this.currentWaypoints.Count > 0)
        {
            var next = this.currentWaypoints[0];
            var last = this.currentWaypoints.Count == 1;
            var reach = last ? FinalTolerance : Tolerance;
            var here = player.PosRot.XYZ();
            if (new Vector2(next.Position.X - here.X, next.Position.Z - here.Z).Length() >= reach)
                break;

            if (next.SpecifiedInPath)
                ++this.navigationProgress;
            this.currentWaypoints.RemoveAt(0);
            if (this.currentWaypoints.Count == 0)
                objective.OnNavigationComplete?.Invoke();
        }

        if (this.currentWaypoints.Count > 0)
            hints.QuestTravel = this.RemainingPath;
    }
}
