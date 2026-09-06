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
    /// <summary>
    /// The party, under the name BossmodReborn modules call it.
    /// <para>They use exactly two things from it — <c>Raid.Player()</c> and <c>Raid.FindSlot(id)</c> — and
    /// between them those accounted for 34 of the compile errors in one batch of 54 ports. An alias costs
    /// nothing and makes that whole class of edit unnecessary.</para>
    /// </summary>
    public PartyState Raid => this.World.Party;

    public WPos Center;
    public ArenaBounds Bounds;

    /// <summary>See the constructor parameter of the same name.</summary>
    public readonly bool OnlyLoadIfTargetable;

    /// <summary>Set by the renderer each frame before drawing; components draw through it.</summary>
    public Arena Arena = null!;

    /// <summary>BossmodReborn's name for <see cref="Arena"/>, used in its drawing code.</summary>
    public Arena MiniArena => this.Arena;

    // --- bounds queries ---
    // Pure geometry over Bounds+Center; Arena exposes the same three, but reaching them through the
    // renderer means a null deref whenever the radar is not drawing. CheckPull, AddHints and AddAIHints
    // all run on the game tick, so that is the common case, not the edge case.

    /// <summary>Is <paramref name="p"/> inside the arena floor?</summary>
    public bool InBounds(WPos p) => this.Bounds.Contains(this.Center, p);

    /// <summary>Distance from <paramref name="origin"/> along <paramref name="dir"/> to the arena edge.</summary>
    public float IntersectRayBounds(WPos origin, WDir dir) => this.Bounds.IntersectRay(this.Center, origin, dir);

    /// <summary>The nearest in-bounds point to <paramref name="position"/> (itself, if already inside).</summary>
    public WPos ClampToBounds(WPos position)
    {
        if (this.InBounds(position))
            return position;
        var dir = position - this.Center;
        var dist = dir.Length();
        if (dist < 1e-6f)
            return position;
        dir /= dist;
        return this.Center + dir * MathF.Min(dist, this.IntersectRayBounds(this.Center, dir));
    }


    private readonly List<ModuleComponent> components = [];
    private readonly EventSubscriptions subscriptions;
    private readonly HashSet<(byte index, uint state)> seenMapEffects = [];
    private StateMachineBuilder? states;
    private int currentPhase = -1;

    /// <param name="onlyLoadIfTargetable">BossmodReborn holds a module back until its boss is targetable,
    /// for fights whose primary actor exists long before the encounter starts. Minerva gates activation on
    /// proximity and combat instead, so this is accepted and recorded rather than acted on — dropping it
    /// from the signature would mean editing every port that passes it.</param>
    protected ModuleBase(WorldState world, Actor primary, WPos center, ArenaBounds bounds, bool onlyLoadIfTargetable = false)
    {
        this.World = world;
        this.PrimaryActor = primary;
        this.Center = center;
        this.Bounds = bounds;
        this.OnlyLoadIfTargetable = onlyLoadIfTargetable;

        this.subscriptions = new EventSubscriptions(
            world.Actors.Added.Subscribe(this.OnActorCreated),
            world.Actors.Removed.Subscribe(this.OnActorDestroyed),
            world.Actors.IsDeadChanged.Subscribe(a => { if (a.IsDead) this.Dispatch(c => c.OnActorDeath(a)); }),
            world.Actors.CastStarted.Subscribe(a =>
            {
                if (a.CastInfo == null)
                    return;
                // the timeline pointer moves on the same event the components see, so a component asking
                // "when is the next raidwide" during OnCastStarted is answered against this cast, not the last
                this.StateMachine.NotifyCastStarted(a.CastInfo.Action.ID, world.CurrentTime);
                this.Dispatch(c => c.OnCastStarted(a, a.CastInfo));
            }),
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
            world.Actors.EventStateChanged.Subscribe((a, v) => this.Dispatch(c => c.OnActorEventStateChange(a, v))),
            world.Actors.RenderflagsChanged.Subscribe((a, f) => this.Dispatch(c => c.OnActorRenderflagsChange(a, f))),
            world.Actors.EStateChanged.Subscribe((a, s) => this.Dispatch(c => c.OnActorEState(a, s))),
            world.Actors.EAnimChanged.Subscribe((a, s) => this.Dispatch(c => c.OnActorEAnim(a, s))),
            world.MapEffect.Subscribe(this.OnMapEffectOp));
    }

    /// <summary>
    /// Instantiate the paired <c>&lt;Name&gt;States</c> builder (which declares the phases) and enter
    /// phase 0, activating its components. Later phases are entered by <see cref="Update"/> as their
    /// transitions fire.
    /// </summary>
    public void BuildStates()
    {
        // Bound by name: <ModuleClass>States. BossmodReborn instead names its states class after the FILE
        // and binds it explicitly with StatesType, which the porter drops -- so a port whose class and file
        // disagree gets no state machine, activates with no components, and looks like a coverage gap
        // rather than a broken binding. ModuleRegistry.UnboundStates() finds those; do not paper over it
        // here by scanning the assembly, which cannot be done safely outside the game.
        var statesType = this.GetType().Assembly.GetType(this.GetType().FullName + "States");
        if (statesType != null)
            this.states = (StateMachineBuilder)Activator.CreateInstance(statesType, this)!;
        this.EnterPhase(0);
    }

    /// <summary>
    /// This module, under the name a component would use for it.
    ///
    /// <para>Components reach their owner as <c>Module</c>, and the porter rewrites BossmodReborn's
    /// <c>Arena.Center</c>/<c>Arena.Bounds</c> to <c>Module.Center</c>/<c>Module.Bounds</c> to match. That
    /// rewrite is right inside a component and wrong inside a MODULE class, where the same members are
    /// already in scope and no <c>Module</c> exists — which broke 23 Occult Crescent modules at once.
    /// Making a module its own <c>Module</c> lets one spelling work in both places, rather than teaching a
    /// regex to tell a component from a module.</para>
    /// </summary>
    protected ModuleBase Module => this;

    /// <summary>Display preferences, under BossmodReborn's name for them. Static because it is a user
    /// setting, not fight state.</summary>
    public static BossModuleConfig WindowConfig => ConfigRoot.Instance.Get<BossModuleConfig>();

    /// <summary>When the current phase began; <c>default</c> before the machine starts.</summary>
    public DateTime PhaseEnteredAt { get; private set; }

    /// <summary>Zero-based index of the phase currently running, or -1 before the machine starts.</summary>
    public int CurrentPhase => this.currentPhase;

    /// <summary>The running machine, under BossmodReborn's name, so ported modules reach
    /// <c>StateMachine.ActivePhaseIndex</c> unchanged.</summary>
    public StateMachine StateMachine => field ??= new StateMachine(this);

    /// <summary>
    /// The declared states in order, or empty when the module has no states class. What
    /// <see cref="StateMachine.NextTransitionWithFlag"/> walks.
    /// </summary>
    public IReadOnlyList<StateMachineBuilder.State> Timeline => this.states?.Timeline ?? [];

    // --- phase machine ---
    private void EnterPhase(int index)
    {
        if (this.states == null || index < 0 || index >= this.states.Phases.Count)
            return;
        this.currentPhase = index;
        this.PhaseEnteredAt = this.World.CurrentTime;
        var phase = this.states.Phases[index];
        foreach (var t in phase.EnterComponents)
            this.ActivateComponent(t);

        // setup a ported module hung on state entry — components activate above, this is everything else
        for (var i = 0; i < phase.EnterActions.Count; ++i)
            phase.EnterActions[i]();
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
    /// <summary>Is any actor of <paramref name="oid"/> in combat? Used by CheckPull to start a module when
    /// a specific enemy engages, rather than when the player does.</summary>
    public bool IsActorInCombat(uint oid)
    {
        foreach (var a in this.World.Actors)
            if (a.OID == oid && a.InCombat && !a.IsDeadOrDestroyed)
                return true;
        return false;
    }

    /// <summary>Any actor of ANY of these types targetable — a phase that ends when whichever add spawned
    /// becomes attackable, without the states file caring which one it was.</summary>
    public bool AnyTargetable(uint[] oids)
    {
        for (var i = 0; i < oids.Length; ++i)
            if (this.AnyTargetable(oids[i]))
                return true;
        return false;
    }

    /// <summary>BossmodReborn's name for <see cref="AnyTargetable(uint)"/>.</summary>
    public bool IsAnyActorTargetable(uint oid) => this.AnyTargetable(oid);

    public bool AnyTargetable(uint oid)
    {
        foreach (var a in this.World.Actors)
            if (a.OID == oid && a.IsTargetable && !a.IsDeadOrDestroyed)
                return true;
        return false;
    }

    // BMR-compatible helpers used by ported modules' phase-completion predicates
    public Actor? GetActor(ulong instanceID) => this.World.Actors.Find(instanceID);

    /// <summary>The first targetable, living candidate, or null.</summary>
    public static Actor? GetActiveActor(List<Actor> candidates)
    {
        for (var i = 0; i < candidates.Count; ++i)
            if (candidates[i].IsTargetable && !candidates[i].IsDead)
                return candidates[i];
        return null;
    }

    /// <summary>The subset that is targetable and alive — the ones a mechanic can actually involve.</summary>
    public static List<Actor> GetActiveActors(List<Actor> candidates)
    {
        var result = new List<Actor>(candidates.Count);
        for (var i = 0; i < candidates.Count; ++i)
            if (candidates[i].IsTargetable && !candidates[i].IsDead)
                result.Add(candidates[i]);
        return result;
    }

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

    public bool IsAnyActorInCombat(uint[] oids)
    {
        foreach (var a in this.World.Actors)
            if (a.InCombat && !a.IsDeadOrDestroyed && Array.IndexOf(oids, a.OID) >= 0)
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

    public bool IsAnyActorInBoundsInCombat(uint[] oids)
    {
        foreach (var a in this.World.Actors)
            if (a.InCombat && !a.IsDeadOrDestroyed && Array.IndexOf(oids, a.OID) >= 0 && this.Bounds.Contains(this.Center, a.Position))
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

    public bool AllDeadOrDestroyedInBounds(uint[] oids)
    {
        foreach (var a in this.World.Actors)
            if (!a.IsDeadOrDestroyed && Array.IndexOf(oids, a.OID) >= 0 && this.Bounds.Contains(this.Center, a.Position))
                return false;
        return true;
    }

    /// <summary>True once map-effect <paramref name="index"/> has been observed in state <paramref name="state"/>.</summary>
    public bool SawMapEffect(byte index, uint state) => this.seenMapEffects.Contains((index, state));

    // --- component management ---
    /// <summary>
    /// Activate a component by type.
    ///
    /// <para>Returns void, as BossmodReborn's does. It used to hand back the instance, which reads as
    /// strictly more useful and is not: a state machine passes this as a METHOD GROUP —
    /// <c>.OnExit(Module.ActivateComponent&lt;CurtainCall&gt;)</c> — and a method with a return value will
    /// not convert to <c>Action</c>. No call site in the tree used the returned instance; the return type
    /// bought nothing and cost the conversion every ported states file depends on.</para>
    /// </summary>
    public void ActivateComponent<T>() where T : ModuleComponent => this.ActivateComponent(typeof(T));

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
    /// <summary>Hook for modules whose arena is not fixed. Called before components update.</summary>
    protected virtual void OnUpdate()
    {
    }

    public void Update()
    {
        this.OnUpdate();
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
        this.DrawArenaForeground(pcSlot, pc);
    }

    /// <summary>Module-level foreground drawing, on top of every component's. Ported modules use this for
    /// per-fight overlays a component would not own — tower markers, tethers drawn from the boss.</summary>
    protected virtual void DrawArenaForeground(int pcSlot, Actor pc) { }

    /// <summary>
    /// Where module errors go. Set once by the plugin to Dalamud's log; left null everywhere else, which is
    /// what the offline validator and the tests want.
    /// </summary>
    public static Action<string>? ErrorSink;

    /// <summary>
    /// A module or component noticed its own state does not make sense — an actor it was tracking vanished,
    /// a cast arrived it could not place. Ported modules call this constantly and it is worth keeping: it is
    /// how a fight tells you its assumptions were wrong, rather than silently drawing the wrong zone.
    /// </summary>
    public void ReportError(ModuleComponent? component, string message)
        => ErrorSink?.Invoke($"[{this.GetType().Name}] [{component?.GetType().Name ?? "module"}] {message}");

    public void ReportError(string message) => this.ReportError(null, message);

    // module-level virtuals BMR's BossModule exposes, so ported modules that override them compile.
    // Minerva does not yet call CheckPull/UpdateModule/CalculateModuleAIHints/CheckReset in its loop, nor
    // read ShouldPrioritizeAllEnemies (tracked). They are declared rather than deleted because a ported
    // module overriding one is recording something real about the fight.
    protected virtual bool CheckPull() => this.PrimaryActor.IsTargetable && this.PrimaryActor.InCombat;

    /// <summary>True when the fight has reset (wipe, or the player left the arena) and the module should
    /// stand down. Recorded, not yet acted on.</summary>
    public virtual bool CheckReset() => false;

    /// <summary>BMR AI hint: treat every enemy as a target rather than just the primary actor. Recorded,
    /// not yet acted on — Minerva's enemy priority comes from components.</summary>
    public virtual bool ShouldPrioritizeAllEnemies => false;
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
        this.SeedPotentialTargets(hints);
        for (var i = 0; i < this.components.Count; ++i)
            this.components[i].AddAIHints(slot, actor, assignment, hints);
    }

    /// <summary>
    /// Fill <see cref="AIHints.PotentialTargets"/> with the hostiles present, so that modules have
    /// something to prioritise.
    ///
    /// <para>Without this the list is empty every frame and <c>PrioritizeTargetsByOID</c>,
    /// <c>SetPriority</c> and friends iterate nothing — 40 modules were declaring "kill the adds first",
    /// "this one is invincible", "interrupt that cast" into a void. Nothing errored; the advice simply did
    /// not exist. Seeding here rather than in each component means a module states its priorities against
    /// the same list every frame, before any component runs.</para>
    ///
    /// <para>Helpers are excluded: they are the invisible actors that cast AOEs on the boss's behalf, and
    /// telling a rotation to attack one is telling it to attack nothing.</para>
    /// </summary>
    private void SeedPotentialTargets(AIHints hints)
    {
        foreach (var a in this.World.Actors)
        {
            if (a.Type != ActorType.Enemy || a.IsAlly || a.IsDeadOrDestroyed || !a.IsTargetable)
                continue;
            hints.PotentialTargets.Add(new AIHints.Enemy(a, a.InCombat ? 0 : AIHints.Enemy.PriorityUndesirable));
        }
    }

    // --- helpers used by components/modules ---
    public DateTime CastFinishAt(ActorCastInfo cast, double extraDelay = 0d) => this.World.FutureTime(cast.RemainingTime + (float)extraDelay);

    /// <summary>As above, but <paramref name="fallback"/> when there is no cast to time from.</summary>
    public DateTime CastFinishAt(ActorCastInfo? cast, double extraDelay, DateTime fallback)
        => cast != null ? this.CastFinishAt(cast, extraDelay) : fallback;

    // returns a List (matching BMR) so callers can use .Count / indexing
    // Enemy lists handed out by Enemies(), kept current for the module's lifetime. Modules follow BMR's
    // idiom of capturing one in a readonly field at construction — `readonly List<Actor> rocks =
    // module.Enemies([...])` — so a plain snapshot silently freezes at activation: anything that spawns
    // after that moment never appears, and the module quietly works from a short list forever. On Treno
    // that meant nine of thirteen boulders cast a line-of-sight shadow and the rest looked like cover
    // the radar refused to mark safe.
    private readonly List<(uint[] OIDs, List<Actor> Actors)> trackedEnemies = [];

    public List<Actor> Enemies(uint oid) => this.Enemies([oid]);

    /// <summary>
    /// Live list of the actors matching these OIDs. The same list instance is returned for the same set of
    /// OIDs and the framework keeps it in step with the world, so callers can hold on to it.
    /// </summary>
    public List<Actor> Enemies(uint[] oids)
    {
        for (var i = 0; i < this.trackedEnemies.Count; ++i)
            if (SameSet(this.trackedEnemies[i].OIDs, oids))
                return this.trackedEnemies[i].Actors;

        var result = new List<Actor>();
        foreach (var a in this.World.Actors)
            if (!a.IsDestroyed && Array.IndexOf(oids, a.OID) >= 0)
                result.Add(a);
        this.trackedEnemies.Add((oids, result));
        return result;
    }

    private static bool SameSet(uint[] a, uint[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (var i = 0; i < a.Length; ++i)
            if (Array.IndexOf(b, a[i]) < 0)
                return false;
        return true;
    }

    private void TrackEnemy(Actor a, bool added)
    {
        for (var i = 0; i < this.trackedEnemies.Count; ++i)
        {
            var (oids, list) = this.trackedEnemies[i];
            if (Array.IndexOf(oids, a.OID) < 0)
                continue;
            if (added)
            {
                if (!list.Contains(a))
                    list.Add(a);
            }
            else
            {
                list.Remove(a);
            }
        }
    }

    /// <summary>
    /// Fan a world event out to every component, containing a throw to the one component that threw.
    ///
    /// <para>Without this, one bad component takes the whole frame down with it. These handlers run inside
    /// the world-state sync, so an exception unwinds past the remaining components, past the rest of the
    /// module, and out of the sync tick — losing actor updates, the AI solve and any recording in progress
    /// for that frame. A single mistake in one fight's module should not stop Minerva mirroring the game.</para>
    ///
    /// <para>The error is reported rather than swallowed: it goes to <see cref="ErrorSink"/> with the
    /// component's name, so it shows up in the log as a specific component misbehaving instead of as a
    /// mechanic that quietly stopped working.</para>
    /// </summary>
    private void Dispatch(Action<ModuleComponent> action)
    {
        for (var i = 0; i < this.components.Count; ++i)
        {
            var component = this.components[i];
            try
            {
                action(component);
            }
            catch (Exception ex)
            {
                this.ReportError(component, $"threw handling a world event: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private void OnActorCreated(Actor a)
    {
        this.TrackEnemy(a, added: true);
        this.Dispatch(c => c.OnActorCreated(a));
    }

    private void OnActorDestroyed(Actor a)
    {
        this.TrackEnemy(a, added: false);
        this.Dispatch(c => c.OnActorDestroyed(a));
    }

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
