namespace Minerva;

/// <summary>
/// Declares a module's phases: which components each phase turns on, and the condition that advances
/// to the next one. A concrete boss pairs with a <c>&lt;Name&gt;States : StateMachineBuilder</c> whose
/// constructor chains <c>Phase(...).ActivateOnEnter&lt;T&gt;().TransitionOn...(...)</c>. Phases run at
/// runtime: <see cref="ModuleBase"/> enters phase 0 when built and, each frame, advances to the next
/// phase once the current phase's transition fires (activating/deactivating components as it goes).
/// A single <see cref="TrivialPhase"/> with no transition reproduces the old "everything on" behaviour.
/// </summary>
public abstract partial class StateMachineBuilder(ModuleBase module)
{
    protected readonly ModuleBase Module = module;

    private readonly List<PhaseDef> phases = [];

    /// <summary>The declared phases, in order. Read by <see cref="ModuleBase"/> to run the machine.</summary>
    public IReadOnlyList<PhaseDef> Phases => this.phases;

    /// <summary>A single phase that stays on for the whole encounter (no transition out).</summary>
    protected PhaseDef TrivialPhase(uint seqID = default, float enrage = 10000f) => this.Phase("Main");

    // convenience predicates for phase transitions (delegate to the module), matching BMR usage
    protected bool AllDeadOrDestroyed(uint oid) => this.Module.AllDeadOrDestroyed(oid);
    protected bool AllDeadOrDestroyed(uint[] oids) => this.Module.AllDeadOrDestroyed(oids);
    protected bool AnyTargetable(uint oid) => this.Module.AnyTargetable(oid);
    protected bool AnyTargetable(uint[] oids) => this.Module.AnyTargetable(oids);
    protected bool IsAnyActorTargetable(uint oid) => this.Module.AnyTargetable(oid);
    protected bool AllDeadOrDestroyedInBounds(uint oid) => this.Module.AllDeadOrDestroyedInBounds(oid);
    protected bool AllDeadOrDestroyedInBounds(uint[] oids) => this.Module.AllDeadOrDestroyedInBounds(oids);

    /// <summary>
    /// Have all of these actually been DESTROYED — despawned, gone from the world?
    ///
    /// <para>Distinct from <see cref="AllDeadOrDestroyed(uint)"/> on purpose: a corpse is dead but still
    /// present, and a phase that waits on destruction is waiting for the bodies to disappear rather than
    /// for the last one to fall. Using the wrong one advances the phase seconds early, while the adds are
    /// still lying there.</para>
    /// </summary>
    protected bool AllDestroyed(uint oid)
    {
        var enemies = this.Module.Enemies(oid);
        for (var i = 0; i < enemies.Count; ++i)
            if (!enemies[i].IsDestroyed)
                return false;
        return true;
    }

    /// <inheritdoc cref="AllDestroyed(uint)"/>
    protected bool AllDestroyed(uint[] oids)
    {
        for (var i = 0; i < oids.Length; ++i)
            if (!this.AllDestroyed(oids[i]))
                return false;
        return true;
    }

    // ---- BossmodReborn state-timeline surface -------------------------------------------------------
    //
    // Fifty-three ported modules build their component lists inside state builders rather than directly on
    // a phase, so without these they do not compile. Every one funnels into the phase the state belongs to:
    // components activate for real, timing does not. See State for why that is the safe direction.
    //
    // Signatures match BossmodReborn exactly — names, argument order and defaults — so a ported file needs
    // no edits at all.

    /// <summary>The phase a bare state attaches to when a module builds states without opening a phase.</summary>
    private PhaseDef CurrentPhase => this.phases.Count > 0 ? this.phases[^1] : this.Phase("Main");

    private readonly List<State> timeline = [];

    /// <summary>
    /// Every declared state, in declaration order. This is the timeline
    /// <see cref="StateMachine.NextTransitionWithFlag"/> walks: for a linear stretch of a fight the next
    /// state declared IS the next state that happens, which is the whole basis of the estimate.
    /// </summary>
    public IReadOnlyList<State> Timeline => this.timeline;

    protected State SimpleState(uint id, float duration, string name)
        => this.Track(new State(this.Module, this.CurrentPhase, id, name, duration));

    private State Track(State s)
    {
        this.timeline.Add(s);
        return s;
    }

    private State Triggered(State s, params uint[] aids)
    {
        s.TriggerAIDs = aids;
        return s;
    }

    protected State Timeout(uint id, float duration, string name = "")
        => this.SimpleState(id, duration, name);

    protected State Condition(uint id, float expected, Func<bool> condition, string name = "", float maxOverdue = 1f, float checkDelay = 0f)
        => this.SimpleState(id, expected, name);

    protected State ComponentCondition<T>(uint id, float expected, Func<T, bool> condition, string name = "", float maxOverdue = 1f, float checkDelay = 0f)
        where T : ModuleComponent
        => this.SimpleState(id, expected, name);

    protected State ActorCastStart(uint id, Func<Actor?> actorAcc, uint aid, float delay, bool isBoss = false, string name = "")
        => this.Triggered(this.SimpleState(id, delay, name), aid);

    protected State ActorCastStartMulti(uint id, Func<Actor?> actorAcc, uint[] aids, float delay, bool isBoss = false, string name = "")
        => this.Triggered(this.SimpleState(id, delay, name), aids);

    /// <summary>
    /// A branch point: whichever action in <paramref name="dispatch"/> the actor starts casting decides
    /// which branch the fight takes. Every branch is built immediately so all of their components activate
    /// on this phase — Minerva's machine cannot pick one at runtime, and a fight that can go three ways
    /// should draw all three rather than none.
    /// </summary>
    protected State ActorCastStartFork(uint id, Func<Actor?> actorAcc, Dictionary<uint, (uint seqID, Action<uint> buildState)> dispatch, float delay, bool isBoss = false, string name = "")
    {
        var s = this.SimpleState(id, delay, name);
        s.IsBranch = true;
        foreach (var (_, (seqID, build)) in dispatch)
            build(seqID);
        return s;
    }

    protected State CastStartFork(uint id, Dictionary<uint, (uint seqID, Action<uint> buildState)> dispatch, float delay, string name = "")
        => this.ActorCastStartFork(id, () => this.Module.PrimaryActor, dispatch, delay, true, name);

    /// <summary>
    /// A branch point chosen by an arbitrary predicate rather than by a cast — the fight looks at something
    /// (a status, a boss's facing, which add spawned) and goes one of several ways.
    ///
    /// <para>Built the same way as <see cref="ActorCastStartFork"/>: every branch immediately, so all of
    /// their components are active on this phase. Minerva's machine has no runtime state selection, and
    /// drawing all three possibilities beats drawing none — the components each key off their own casts, so
    /// the branches that did not happen simply never fire.</para>
    ///
    /// <para><paramref name="condition"/> and <paramref name="select"/> are accepted and not called. They
    /// are how BossmodReborn picks at runtime; keeping them in the signature is what lets a ported file
    /// compile unedited, and calling them here would evaluate a fight's branch predicate at build time,
    /// which is both meaningless and a good way to throw inside a constructor.</para>
    /// </summary>
    protected State ConditionFork<TKey>(uint id, float expected, Func<bool> condition, Func<TKey> select,
        Dictionary<TKey, (uint seqID, Action<uint> buildState)> dispatch, string name = "")
        where TKey : notnull
    {
        var s = this.SimpleState(id, expected, name);
        s.IsBranch = true;
        foreach (var (_, (seqID, build)) in dispatch)
            build(seqID);
        return s;
    }

    /// <inheritdoc cref="ConditionFork{TKey}"/>
    protected State ComponentConditionFork<T, TKey>(uint id, float expected, Func<T, bool> condition, Func<T, TKey> select,
        Dictionary<TKey, (uint seqID, Action<uint> buildState)> dispatch, string name = "")
        where T : ModuleComponent
        where TKey : notnull
    {
        var s = this.SimpleState(id, expected, name);
        s.IsBranch = true;
        foreach (var (_, (seqID, build)) in dispatch)
            build(seqID);
        return s;
    }

    protected State ActorCastEnd(uint id, Func<Actor?> actorAcc, float castTime, bool isBoss = false, string name = "", bool interruptible = false)
        => this.SimpleState(id, castTime, name);

    protected State ActorCast(uint id, Func<Actor?> actorAcc, uint aid, float delay, float castTime, bool isBoss = false, string name = "", bool interruptible = false)
        => this.Triggered(this.SimpleState(id, delay + castTime, name), aid);

    protected State ActorCastMulti(uint id, Func<Actor?> actorAcc, uint[] aids, float delay, float castTime, bool isBoss = false, string name = "", bool interruptible = false)
        => this.Triggered(this.SimpleState(id, delay + castTime, name), aids);

    protected State ActorTargetable(uint id, Func<Actor?> actorAcc, bool targetable, float delay, string name = "", float checkDelay = 0f)
        => this.SimpleState(id, delay, name);

    /// <summary>
    /// A named phase whose states are built by <paramref name="buildState"/>.
    /// <para>The callback is invoked immediately so its <c>ActivateOnEnter</c> calls land on this phase —
    /// which is the whole reason this surface exists.</para>
    /// </summary>
    protected PhaseDef SimplePhase(uint seqID, Action<uint> buildState, string name, float dur = -1f)
    {
        var p = this.Phase(string.IsNullOrEmpty(name) ? $"P{seqID}" : name);
        buildState(seqID);
        return p;
    }

    protected PhaseDef DeathPhase(uint seqID, Action<uint> buildState) => this.SimplePhase(seqID, buildState, "Main");

    // the same states aimed at the primary actor, which is how most modules write them
    protected State CastStart(uint id, uint aid, float delay, string name = "")
        => this.Triggered(this.SimpleState(id, delay, name), aid);

    protected State CastStartMulti(uint id, uint[] aids, float delay, string name = "")
        => this.Triggered(this.SimpleState(id, delay, name), aids);

    protected State CastEnd(uint id, float castTime, string name = "", bool interruptible = false)
        => this.SimpleState(id, castTime, name);

    protected State Cast(uint id, uint aid, float delay, float castTime, string name = "", bool interruptible = false)
        => this.Triggered(this.SimpleState(id, delay + castTime, name), aid);

    protected State CastMulti(uint id, uint[] aids, float delay, float castTime, string name = "", bool interruptible = false)
        => this.Triggered(this.SimpleState(id, delay + castTime, name), aids);

    protected State Targetable(uint id, bool targetable, float delay, string name = "", float checkDelay = 0f)
        => this.SimpleState(id, delay, name);

    /// <summary>Begin a named phase; chain <c>ActivateOnEnter</c>/<c>TransitionOn*</c> to define it.</summary>
    protected PhaseDef Phase(string name)
    {
        var p = new PhaseDef(this.Module, name);
        this.phases.Add(p);
        return p;
    }

    /// <summary>One phase: the components it activates on entry and the condition that ends it.</summary>
    /// <summary>Raw per-state hooks, mirroring BMR's <c>state.Raw</c>. Ported modules set <c>Raw.Update</c>
    /// to a completion predicate; Minerva uses it as the phase transition when no explicit one is set.</summary>
    public sealed class RawState
    {
        public Func<bool>? Update;
    }

    public sealed class PhaseDef(ModuleBase module, string name)
    {
        public readonly string Name = name;
        public readonly List<Type> EnterComponents = [];
        public readonly RawState Raw = new();

        private Func<bool>? transition;

        /// <summary>Evaluated each frame while this phase is current; when true, the machine advances.
        /// Falls back to <see cref="RawState.Update"/> when no explicit transition was set.</summary>
        public Func<bool>? Transition => this.transition ?? this.Raw.Update;

        /// <summary>Turn component <typeparamref name="T"/> on when this phase is entered.</summary>
        public readonly List<Action> EnterActions = [];

        /// <summary>Run an arbitrary action when this phase begins. Ported states hang setup code here.</summary>
        public PhaseDef OnEnter(Action action)
        {
            this.EnterActions.Add(action);
            return this;
        }

        /// <summary>Turn component <typeparamref name="T"/> on when this phase is entered. The
        /// <paramref name="condition"/> overload lets a ported module gate a component on a difficulty or
        /// a config choice, which is how BossmodReborn spells it.</summary>
        public PhaseDef ActivateOnEnter<T>(bool condition = true) where T : ModuleComponent
        {
            if (condition)
                this.EnterComponents.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Ignored, for the same reason <see cref="State.DeactivateOnEnter{T}"/> is: Minerva's machine is
        /// phases only, so the nearest honest meaning of "deactivate" here is "off for the whole phase" —
        /// which would turn a mechanic that fires later in the phase into one that is never drawn. An
        /// over-active component draws a zone before it is live; it never hides one that is.
        /// </summary>
        public PhaseDef DeactivateOnEnter<T>(bool condition = true) where T : ModuleComponent => this;

        /// <summary>Ignored, as <see cref="DeactivateOnEnter{T}"/> is.</summary>
        public PhaseDef DeactivateOnExit<T>(bool condition = true) where T : ModuleComponent => this;

        /// <summary>Runs on phase entry — there is no exit instant to hang it on.</summary>
        public PhaseDef OnExit(Action action) => this.OnEnter(action);

        /// <summary>Hint flags for this phase (currently just "starts in downtime"). Recorded, not enforced.</summary>
        public StateMachine.PhaseHint Hints { get; private set; }

        public PhaseDef SetHint(StateMachine.PhaseHint h, bool condition = true)
        {
            if (condition)
                this.Hints |= h;
            return this;
        }

        /// <summary>Advance to the next phase when <paramref name="condition"/> first becomes true.</summary>
        public PhaseDef TransitionOn(Func<bool> condition)
        {
            this.transition = condition;
            return this;
        }

        /// <summary>Advance when any actor of <paramref name="oid"/> is present and targetable (a boss form appearing).</summary>
        public PhaseDef TransitionOnTargetable(uint oid) => this.TransitionOn(() => module.AnyTargetable(oid));

        /// <summary>Advance when the primary actor's HP falls to or below <paramref name="hpFraction"/> (0..1).</summary>
        public PhaseDef TransitionOnPrimaryHP(float hpFraction) => this.TransitionOn(() => module.PrimaryActor.HPRatio <= hpFraction);

        /// <summary>Advance once map-effect <paramref name="index"/> has been seen in state <paramref name="state"/> (an arena change).</summary>
        public PhaseDef TransitionOnMapEffect(byte index, uint state) => this.TransitionOn(() => module.SawMapEffect(index, state));
    }
}
