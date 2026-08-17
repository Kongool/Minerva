namespace Minerva;

/// <summary>
/// Base class for an encounter module: owns the arena geometry, the set of active
/// <see cref="ModuleComponent"/>s, and the associated state machine; subscribes to world events
/// once and fans them out to components. A concrete boss subclasses this, defines OID/AID enums and
/// component one-liners, and pairs with a <c>&lt;Name&gt;States</c> <see cref="StateMachineBuilder"/>.
/// </summary>
public abstract class ModuleBase : IDisposable
{
    public readonly WorldState World;
    public readonly Actor PrimaryActor;
    public WPos Center;
    public ArenaBounds Bounds;

    /// <summary>Set by the renderer each frame before drawing; components draw through it.</summary>
    public Arena Arena = null!;

    private readonly List<ModuleComponent> components = [];
    private readonly EventSubscriptions subscriptions;
    private readonly HashSet<(byte index, uint state)> seenMapEffects = [];
    private StateMachineBuilder? states;
    private int currentPhase = -1;

    protected ModuleBase(WorldState world, Actor primary, WPos center, ArenaBounds bounds)
    {
        this.World = world;
        this.PrimaryActor = primary;
        this.Center = center;
        this.Bounds = bounds;

        this.subscriptions = new EventSubscriptions(
            world.Actors.Added.Subscribe(this.OnActorCreated),
            world.Actors.Removed.Subscribe(this.OnActorDestroyed),
            world.Actors.IsDeadChanged.Subscribe(a => { if (a.IsDead) this.Dispatch(c => c.OnActorDeath(a)); }),
            world.Actors.CastStarted.Subscribe(a => { if (a.CastInfo != null) this.Dispatch(c => c.OnCastStarted(a, a.CastInfo)); }),
            world.Actors.CastFinished.Subscribe((a, cast) => this.Dispatch(c => c.OnCastFinished(a, cast))),
            world.Actors.CastEvent.Subscribe((a, e) => this.Dispatch(c => c.OnEventCast(a, e))),
            world.Actors.StatusGain.Subscribe(this.DispatchStatusGain),
            world.Actors.StatusLose.Subscribe(this.DispatchStatusLose),
            world.Actors.Tethered.Subscribe(this.DispatchTethered),
            world.Actors.Untethered.Subscribe(this.DispatchUntethered),
            world.Actors.IconAppeared.Subscribe((a, e) => this.Dispatch(c => c.OnEventIcon(a, e.IconID, e.TargetID))),
            world.Actors.VFXAppeared.Subscribe((a, e) => this.Dispatch(c => c.OnEventVFX(a, e.VFXID, e.TargetID))),
            world.Actors.ModelStateChanged.Subscribe((a, s) => this.Dispatch(c => c.OnActorModelStateChange(a, s, 0, 0))),
            world.Actors.ActionTimelineEvent.Subscribe((a, id) => this.Dispatch(c => c.OnActorPlayActionTimelineEvent(a, id))),
            world.MapEffect.Subscribe(this.OnMapEffectOp));
    }

    /// <summary>
    /// Instantiate the paired <c>&lt;Name&gt;States</c> builder (which declares the phases) and enter
    /// phase 0, activating its components. Later phases are entered by <see cref="Update"/> as their
    /// transitions fire.
    /// </summary>
    public void BuildStates()
    {
        var statesTypeName = this.GetType().FullName + "States";
        var statesType = this.GetType().Assembly.GetType(statesTypeName);
        if (statesType != null)
            this.states = (StateMachineBuilder)Activator.CreateInstance(statesType, this)!;
        this.EnterPhase(0);
    }

    /// <summary>Zero-based index of the phase currently running, or -1 before the machine starts.</summary>
    public int CurrentPhase => this.currentPhase;

    // --- phase machine ---
    private void EnterPhase(int index)
    {
        if (this.states == null || index < 0 || index >= this.states.Phases.Count)
            return;
        this.currentPhase = index;
        foreach (var t in this.states.Phases[index].EnterComponents)
            this.ActivateComponent(t);
    }

    private void ExitPhase()
    {
        if (this.states == null || this.currentPhase < 0 || this.currentPhase >= this.states.Phases.Count)
            return;
        foreach (var t in this.states.Phases[this.currentPhase].EnterComponents)
            this.DeactivateComponent(t);
    }

    // advance through as many phases as fire this frame (a guard prevents a bad predicate from looping forever)
    private void AdvancePhase()
    {
        if (this.states == null)
            return;
        for (var guard = 0; this.currentPhase >= 0 && this.currentPhase < this.states.Phases.Count - 1 && guard < 16; ++guard)
        {
            var transition = this.states.Phases[this.currentPhase].Transition;
            if (transition == null || !transition())
                break;
            this.ExitPhase();
            this.EnterPhase(this.currentPhase + 1);
        }
    }

    /// <summary>True if any actor of <paramref name="oid"/> exists in the world and is targetable.</summary>
    public bool AnyTargetable(uint oid)
    {
        foreach (var a in this.World.Actors)
            if (a.OID == oid && a.IsTargetable && !a.IsDeadOrDestroyed)
                return true;
        return false;
    }

    // BMR-compatible helpers used by ported modules' phase-completion predicates
    public Actor? GetActor(ulong instanceID) => this.World.Actors.Find(instanceID);

    public bool AllDeadOrDestroyed(uint oid)
    {
        foreach (var a in this.World.Actors)
            if (a.OID == oid && !a.IsDeadOrDestroyed)
                return false;
        return true;
    }

    public bool AllDeadOrDestroyed(uint[] oids)
    {
        foreach (var a in this.World.Actors)
            if (!a.IsDeadOrDestroyed && Array.IndexOf(oids, a.OID) >= 0)
                return false;
        return true;
    }

    public bool IsAnyActorInCombat(uint oid)
    {
        foreach (var a in this.World.Actors)
            if (a.OID == oid && a.InCombat && !a.IsDeadOrDestroyed)
                return true;
        return false;
    }

    public bool IsAnyActorInBoundsInCombat(uint oid)
    {
        foreach (var a in this.World.Actors)
            if (a.OID == oid && a.InCombat && !a.IsDeadOrDestroyed && this.Bounds.Contains(this.Center, a.Position))
                return true;
        return false;
    }

    public bool AllDeadOrDestroyedInBounds(uint oid)
    {
        foreach (var a in this.World.Actors)
            if (a.OID == oid && !a.IsDeadOrDestroyed && this.Bounds.Contains(this.Center, a.Position))
                return false;
        return true;
    }

    /// <summary>True once map-effect <paramref name="index"/> has been observed in state <paramref name="state"/>.</summary>
    public bool SawMapEffect(byte index, uint state) => this.seenMapEffects.Contains((index, state));

    // --- component management ---
    public T ActivateComponent<T>() where T : ModuleComponent => (T)this.ActivateComponent(typeof(T));

    public ModuleComponent ActivateComponent(Type componentType)
    {
        var existing = this.FindComponent(componentType);
        if (existing != null)
            return existing;
        var comp = (ModuleComponent)Activator.CreateInstance(componentType, this)!;
        this.components.Add(comp);
        return comp;
    }

    public void DeactivateComponent<T>() where T : ModuleComponent => this.DeactivateComponent(typeof(T));

    public void DeactivateComponent(Type componentType)
    {
        var c = this.FindComponent(componentType);
        if (c != null)
            this.components.Remove(c);
    }

    private ModuleComponent? FindComponent(Type componentType)
    {
        for (var i = 0; i < this.components.Count; ++i)
            if (componentType.IsInstanceOfType(this.components[i]))
                return this.components[i];
        return null;
    }

    public T? FindComponent<T>() where T : ModuleComponent
    {
        for (var i = 0; i < this.components.Count; ++i)
            if (this.components[i] is T t)
                return t;
        return null;
    }

    public IReadOnlyList<ModuleComponent> Components => this.components;

    // --- per-frame ---
    public void Update()
    {
        this.AdvancePhase();
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].Update();
    }

    public void DrawArena(int pcSlot, Actor pc)
    {
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].DrawArenaBackground(pcSlot, pc);
        this.Arena.DrawBoundary();
        this.DrawEnemies(pcSlot, pc);
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].DrawArenaForeground(pcSlot, pc);
    }

    // module-level virtuals BMR's BossModule exposes, so ported modules that override them compile.
    // Minerva does not yet call CheckPull/UpdateModule/CalculateModuleAIHints in its loop (tracked).
    protected virtual bool CheckPull() => this.PrimaryActor.IsTargetable && this.PrimaryActor.InCombat;
    protected virtual void UpdateModule() { }
    protected virtual void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) { }

    /// <summary>Draw the boss + tracked objects. Override to customise; default draws the primary actor.</summary>
    protected virtual void DrawEnemies(int pcSlot, Actor pc)
    {
        if (!this.PrimaryActor.IsDeadOrDestroyed)
            this.Arena.ActorMarker(this.PrimaryActor.Position, this.PrimaryActor.Rotation, this.PrimaryActor.HitboxRadius, Colors.Enemy);
    }

    public void AddGlobalHints(ModuleComponent.GlobalHints hints)
    {
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].AddGlobalHints(hints);
    }

    public void AddHints(int slot, Actor actor, ModuleComponent.TextHints hints)
    {
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].AddHints(slot, actor, hints);
    }

    /// <summary>Populate the auto-dodge hints from every component, for the given player. The party
    /// role <paramref name="assignment"/> is passed through to components (Unassigned by default, since
    /// Minerva has no role-config UI yet).</summary>
    public void BuildAIHints(int slot, Actor actor, AIHints hints, PartyRolesConfig.Assignment assignment = PartyRolesConfig.Assignment.Unassigned)
    {
        hints.Clear();
        hints.PlayerPosition = actor.Position;
        hints.Center = this.Center;
        hints.Bounds = this.Bounds;
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].AddAIHints(slot, actor, assignment, hints);
    }

    // --- helpers used by components/modules ---
    public DateTime CastFinishAt(ActorCastInfo cast, double extraDelay = 0d) => this.World.FutureTime(cast.RemainingTime + (float)extraDelay);

    // returns a List (matching BMR) so callers can use .Count / indexing
    public List<Actor> Enemies(uint oid)
    {
        var result = new List<Actor>();
        foreach (var a in this.World.Actors)
            if (a.OID == oid && !a.IsDestroyed)
                result.Add(a);
        return result;
    }

    public List<Actor> Enemies(uint[] oids)
    {
        var result = new List<Actor>();
        foreach (var a in this.World.Actors)
            if (!a.IsDestroyed && Array.IndexOf(oids, a.OID) >= 0)
                result.Add(a);
        return result;
    }

    private void Dispatch(Action<ModuleComponent> action)
    {
        for (var i = 0; i < this.components.Count; ++i)
            action(this.components[i]);
    }

    private void OnActorCreated(Actor a) => this.Dispatch(c => c.OnActorCreated(a));
    private void OnActorDestroyed(Actor a) => this.Dispatch(c => c.OnActorDestroyed(a));

    // status/tether events carry the struct by ref/in (matching BMR), which can't be captured in a
    // lambda, so these fan out directly. The status slot holds the relevant status at fire time (the
    // gained status after a gain; the still-present lost status before a lose — see OpStatus).
    private void DispatchStatusGain(Actor a, int index)
    {
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].OnStatusGain(a, ref a.Statuses[index]);
    }

    private void DispatchStatusLose(Actor a, int index)
    {
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].OnStatusLose(a, ref a.Statuses[index]);
    }

    private void DispatchTethered(Actor a)
    {
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].OnTethered(a, in a.Tether);
    }

    private void DispatchUntethered(Actor a)
    {
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].OnUntethered(a, in a.Tether);
    }

    private void OnMapEffectOp(WorldState.OpMapEffect op)
    {
        this.seenMapEffects.Add((op.Index, op.State));
        this.Dispatch(c => c.OnMapEffect(op.Index, op.State));
    }

    public void Dispose()
    {
        this.subscriptions.Dispose();
        GC.SuppressFinalize(this);
    }
}
