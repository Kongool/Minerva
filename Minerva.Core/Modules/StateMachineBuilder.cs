namespace Minerva;

/// <summary>
/// Declares a module's phases and the states within them. A concrete boss pairs with a
/// <c>&lt;Name&gt;States : StateMachineBuilder</c> whose constructor either opens a phase and switches components on
/// (<c>TrivialPhase().ActivateOnEnter&lt;T&gt;()</c>), or lays the fight out as BossmodReborn does: phases of
/// states, each waiting on a cast, a component or a timeout and switching components on and off as it goes. The
/// machine that runs them is <see cref="StateMachine"/>.
/// <para>Names, argument order and defaults match BossmodReborn's builder, so a ported states file compiles unedited.
/// Action ids are plain <c>uint</c>, which is how the porter writes them.</para>
/// </summary>
public abstract partial class StateMachineBuilder(ModuleBase module)
{
    protected readonly ModuleBase Module = module;

    private readonly List<PhaseDef> phases = [];
    private readonly List<State> timeline = [];
    private readonly HashSet<uint> stateIds = [];

    // BossmodReborn's chain-building cursor: the first state of the chain being built, and its last
    private State? curInitial;
    private State? lastState;
    private bool chaining; // inside SimplePhase or a fork's branch, where the cursor above links states

    /// <summary>The declared phases, in order. Read by <see cref="ModuleBase"/> to run the machine.</summary>
    public IReadOnlyList<PhaseDef> Phases => this.phases;

    /// <summary>Every declared state, in declaration order.</summary>
    public IReadOnlyList<State> Timeline => this.timeline;

    /// <summary>A single phase that stays on for the whole encounter, with no states and no transition out.</summary>
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

    // ---- phases -------------------------------------------------------------------------------------

    /// <summary>Begin a named phase with no states; chain <c>ActivateOnEnter</c>/<c>TransitionOn*</c> to define it.</summary>
    protected PhaseDef Phase(string name)
    {
        var p = new PhaseDef(this.Module, name);
        this.phases.Add(p);
        return p;
    }

    /// <summary>A named phase whose states <paramref name="buildState"/> builds, from id <c>seqID &lt;&lt; 24</c>.</summary>
    protected PhaseDef SimplePhase(uint seqID, Action<uint> buildState, string name, float dur = -1f)
    {
        var p = this.Phase(string.IsNullOrEmpty(name) ? $"P{seqID}" : name);
        var (wasChaining, prevInitial, prevLast) = (this.chaining, this.curInitial, this.lastState);
        this.chaining = true;
        this.curInitial = this.lastState = null;
        buildState(seqID << 24);
        p.InitialState = this.curInitial;
        (this.chaining, this.curInitial, this.lastState) = (wasChaining, prevInitial, prevLast);
        return p;
    }

    /// <summary>A phase that ends when the primary actor dies: BossmodReborn's usual single-phase fight, and the first
    /// phase of a fight whose first boss gives way to a second.</summary>
    protected PhaseDef DeathPhase(uint seqID, Action<uint> buildState)
    {
        var p = this.SimplePhase(seqID, buildState, "Main");
        p.EndOnDeath();
        return p;
    }

    // ---- states -------------------------------------------------------------------------------------

    /// <summary>A state with no transition of its own; the module sets <c>Raw.Update</c>, or it is the end of a chain.</summary>
    protected State SimpleState(uint id, float duration, string name)
    {
        while (!this.stateIds.Add(id))
            ++id; // BossmodReborn's rule for a duplicate id

        var phase = this.phases.Count > 0 ? this.phases[^1] : this.Phase("Main");
        var state = new State(this.Module, phase, id, name, duration);
        this.timeline.Add(state);

        if (this.chaining)
        {
            if (this.lastState != null)
                this.Link(this.lastState, state);
            else if (this.curInitial == null)
                this.curInitial = state;
            else
                this.Module.ReportError($"State {id:X} follows a fork in its chain and cannot be reached");
            this.lastState = state;
        }
        else
        {
            // a state declared straight onto a phase opened without SimplePhase: it chains onto that phase
            if (phase.LastState != null)
                this.Link(phase.LastState, state);
            else if (phase.InitialState == null)
                phase.InitialState = state;
            else
                this.Module.ReportError($"State {id:X} follows a fork in its phase and cannot be reached");
            phase.LastState = state;
        }
        return state;
    }

    private void Link(State from, State to)
    {
        if (from.NextStates != null)
        {
            this.Module.ReportError($"State {from.ID:X} is already linked while adding state {to.ID:X}");
            return;
        }
        from.NextStates = [to];
        if ((from.ID & 0xFFFF0000) == (to.ID & 0xFFFF0000))
            from.SetHint(StateMachine.StateHint.GroupWithNext);
    }

    /// <summary>A state that ends when its duration has passed.</summary>
    protected State Timeout(uint id, float duration, string name = "")
    {
        var state = this.SimpleState(id, duration, name);
        state.Raw.Comment = "Timeout";
        state.Raw.Update = t => t >= state.Raw.Duration ? 0 : -1;
        return state;
    }

    /// <summary>A state that ends when <paramref name="condition"/> holds, or when it is <paramref name="maxOverdue"/> late.</summary>
    protected State Condition(uint id, float expected, Func<bool> condition, string name = "", float maxOverdue = 1f, float checkDelay = 0f)
    {
        var state = this.SimpleState(id, expected, name);
        state.Raw.Comment = "Generic condition";
        state.Raw.Update = t =>
        {
            if (t < checkDelay)
                return -1;
            if (condition())
                return 0;
            if (t < expected + maxOverdue)
                return -1;
            this.Module.ReportError($"State {id:X}: transition triggered because of overdue");
            return 0;
        };
        return state;
    }

    /// <summary>A state that ends when a component's condition holds; a missing component ends it at once.</summary>
    protected State ComponentCondition<T>(uint id, float expected, Func<T, bool> condition, string name = "", float maxOverdue = 1f, float checkDelay = 0f)
        where T : ModuleComponent
    {
        var state = this.SimpleState(id, expected, name);
        state.Raw.Comment = $"Condition on {typeof(T).Name}";
        state.Raw.Update = t =>
        {
            if (t < checkDelay)
                return -1;
            if (this.Module.FindComponent<T>() is not { } comp)
            {
                this.Module.ReportError($"State {id:X}: component {typeof(T).Name} needed for condition is missing");
                return 0;
            }
            if (condition(comp))
                return 0;
            if (t < expected + maxOverdue)
                return -1;
            this.Module.ReportError($"State {id:X}: transition triggered because of overdue");
            return 0;
        };
        return state;
    }

    /// <summary>
    /// A branch point: when <paramref name="condition"/> holds, <paramref name="select"/> picks which of the chains in
    /// <paramref name="dispatch"/> the fight continues down. Each branch is its own chain, built here.
    /// </summary>
    protected State ConditionFork<TKey>(uint id, float expected, Func<bool> condition, Func<TKey> select,
        Dictionary<TKey, (uint seqID, Action<uint> buildState)> dispatch, string name = "")
        where TKey : notnull
    {
        var state = this.SimpleState(id, expected, name);
        state.Raw.Comment = $"Fork: [{string.Join(", ", dispatch.Keys)}]";
        var branches = new State[dispatch.Count];
        var index = new Dictionary<TKey, int>();
        state.Raw.Update = _ =>
        {
            if (!condition())
                return -1;
            var key = select();
            if (!index.TryGetValue(key, out var fork))
            {
                this.Module.ReportError($"State {id:X}: unexpected fork condition result: got {key}");
                return -1;
            }
            return fork;
        };

        var (wasChaining, prevInitial) = (this.chaining, this.curInitial);
        var i = 0;
        foreach (var (key, (seqID, build)) in dispatch)
        {
            this.chaining = true;
            this.curInitial = this.lastState = null;
            build(seqID << 24);
            if (this.curInitial == null)
                this.Module.ReportError($"Fork {i} of state {id:X} built no states");
            branches[i] = this.curInitial!;
            index[key] = i++;
        }
        state.NextStates = branches;

        // nothing links after a fork: each branch carries the fight from here
        (this.chaining, this.curInitial, this.lastState) = (wasChaining, prevInitial, null);
        if (!wasChaining)
            state.Phase.LastState = null;
        return state;
    }

    /// <inheritdoc cref="ConditionFork{TKey}"/>
    protected State ComponentConditionFork<T, TKey>(uint id, float expected, Func<T, bool> condition, Func<T, TKey> select,
        Dictionary<TKey, (uint seqID, Action<uint> buildState)> dispatch, string name = "")
        where T : ModuleComponent
        where TKey : notnull
    {
        bool Holds()
        {
            if (this.Module.FindComponent<T>() is { } comp)
                return condition(comp);
            this.Module.ReportError($"State {id:X}: component {typeof(T).Name} needed for condition is missing");
            return false;
        }
        return this.ConditionFork(id, expected, Holds, () => select(this.Module.FindComponent<T>()!), dispatch, name);
    }

    /// <summary>A state that ends when the actor starts casting; an unexpected cast still ends it, and says so.</summary>
    protected State ActorCastStart(uint id, Func<Actor?> actorAcc, uint aid, float delay, bool isBoss = false, string name = "")
    {
        var state = this.SimpleState(id, delay, name).SetHint(StateMachine.StateHint.BossCastStart, isBoss);
        state.Raw.Comment = $"Cast start: {aid}";
        state.Raw.Update = _ =>
        {
            if (actorAcc()?.CastInfo is not { } cast)
                return -1;
            if (cast.Action.ID != aid)
                this.Module.ReportError($"State {id:X}: unexpected cast start: got {cast.Action.ID}, expected {aid}");
            return 0;
        };
        return state;
    }

    protected State ActorCastStartMulti(uint id, Func<Actor?> actorAcc, uint[] aids, float delay, bool isBoss = false, string name = "")
    {
        var state = this.SimpleState(id, delay, name).SetHint(StateMachine.StateHint.BossCastStart, isBoss);
        state.Raw.Comment = $"Cast start: [{string.Join(", ", aids)}]";
        state.Raw.Update = _ =>
        {
            if (actorAcc()?.CastInfo is not { } cast)
                return -1;
            if (Array.IndexOf(aids, cast.Action.ID) < 0)
                this.Module.ReportError($"State {id:X}: unexpected cast start: got {cast.Action.ID}");
            return 0;
        };
        return state;
    }

    /// <summary>A branch on which of <paramref name="dispatch"/>'s actions the actor starts casting.</summary>
    protected State ActorCastStartFork(uint id, Func<Actor?> actorAcc, Dictionary<uint, (uint seqID, Action<uint> buildState)> dispatch, float delay, bool isBoss = false, string name = "")
        => this.ConditionFork(id, delay, () => actorAcc()?.CastInfo?.IsSpell() ?? false, () => actorAcc()!.CastInfo!.Action.ID, dispatch, name)
            .SetHint(StateMachine.StateHint.BossCastStart, isBoss);

    protected State CastStartFork(uint id, Dictionary<uint, (uint seqID, Action<uint> buildState)> dispatch, float delay, string name = "")
        => this.ActorCastStartFork(id, () => this.Module.PrimaryActor, dispatch, delay, true, name);

    /// <summary>A state that ends when the actor stops casting (for an interruptible cast, not before its time).</summary>
    protected State ActorCastEnd(uint id, Func<Actor?> actorAcc, float castTime, bool isBoss = false, string name = "", bool interruptible = false)
    {
        var state = this.SimpleState(id, castTime, name).SetHint(StateMachine.StateHint.BossCastEnd, isBoss);
        state.Raw.Comment = interruptible ? "Interruptible cast end" : "Cast end";
        state.Raw.Update = t => actorAcc()?.CastInfo == null && (!interruptible || t >= state.Raw.Duration) ? 0 : -1;
        return state;
    }

    /// <summary>Cast start then cast end; the second state takes id + 1 and the name.</summary>
    protected State ActorCast(uint id, Func<Actor?> actorAcc, uint aid, float delay, float castTime, bool isBoss = false, string name = "", bool interruptible = false)
    {
        this.ActorCastStart(id, actorAcc, aid, delay, isBoss, "");
        return this.ActorCastEnd(id + 1, actorAcc, castTime, isBoss, name, interruptible);
    }

    protected State ActorCastMulti(uint id, Func<Actor?> actorAcc, uint[] aids, float delay, float castTime, bool isBoss = false, string name = "", bool interruptible = false)
    {
        this.ActorCastStartMulti(id, actorAcc, aids, delay, isBoss, "");
        return this.ActorCastEnd(id + 1, actorAcc, castTime, isBoss, name, interruptible);
    }

    /// <summary>A state that ends when the actor becomes (un)targetable.</summary>
    protected State ActorTargetable(uint id, Func<Actor?> actorAcc, bool targetable, float delay, string name = "", float checkDelay = 0f)
    {
        var state = this.SimpleState(id, delay, name);
        state.Raw.Comment = targetable ? "Targetable" : "Untargetable";
        state.Raw.Update = t => t >= checkDelay && actorAcc()?.IsTargetable == targetable ? 0 : -1;
        return state;
    }

    // the same states aimed at the primary actor, which is how most modules write them
    protected State CastStart(uint id, uint aid, float delay, string name = "")
        => this.ActorCastStart(id, () => this.Module.PrimaryActor, aid, delay, true, name);

    protected State CastStartMulti(uint id, uint[] aids, float delay, string name = "")
        => this.ActorCastStartMulti(id, () => this.Module.PrimaryActor, aids, delay, true, name);

    protected State CastEnd(uint id, float castTime, string name = "", bool interruptible = false)
        => this.ActorCastEnd(id, () => this.Module.PrimaryActor, castTime, true, name, interruptible);

    protected State Cast(uint id, uint aid, float delay, float castTime, string name = "", bool interruptible = false)
        => this.ActorCast(id, () => this.Module.PrimaryActor, aid, delay, castTime, true, name, interruptible);

    protected State CastMulti(uint id, uint[] aids, float delay, float castTime, string name = "", bool interruptible = false)
        => this.ActorCastMulti(id, () => this.Module.PrimaryActor, aids, delay, castTime, true, name, interruptible);

    protected State Targetable(uint id, bool targetable, float delay, string name = "", float checkDelay = 0f)
        => this.ActorTargetable(id, () => this.Module.PrimaryActor, targetable, delay, name, checkDelay)
            .SetHint(targetable ? StateMachine.StateHint.DowntimeEnd : StateMachine.StateHint.DowntimeStart);

    /// <summary>Raw per-phase hooks, mirroring BMR's <c>phase.Raw</c>. Ported modules set <c>Raw.Update</c> to the
    /// phase's end; Minerva uses it as the phase transition when no explicit one is set.</summary>
    public sealed class RawState
    {
        public Func<bool>? Update;
    }

    /// <summary>One phase: what it switches on and runs when it begins and ends, its states, and the condition that ends it.</summary>
    public sealed class PhaseDef(ModuleBase module, string name)
    {
        public readonly string Name = name;

        /// <summary>The components this phase switches on when it begins, for anything that wants to list them.</summary>
        public readonly List<Type> EnterComponents = [];

        public readonly RawState Raw = new();

        /// <summary>Where the phase's state walk begins; null for a phase with no states.</summary>
        public State? InitialState { get; internal set; }

        internal State? LastState; // the end of the chain states declared straight onto this phase extend
        internal Action? Enter;
        internal Action? Exit;

        private Func<bool>? transition;
        private Func<bool>? death;

        /// <summary>Evaluated each frame while this phase is current; when true, the machine advances.
        /// Falls back to <see cref="RawState.Update"/> when no explicit transition was set.</summary>
        public Func<bool>? Transition => this.transition ?? this.Raw.Update;

        /// <summary>Is this phase's end only its boss dying (<c>DeathPhase</c>, not overridden)? Then, as the last phase,
        /// it is not the module's own end: the boss's death already ends it.</summary>
        public bool EndsOnDeath => this.transition == null && this.death != null && this.Raw.Update == this.death;

        internal void EndOnDeath()
        {
            // BossmodReborn's DeathPhase; an HP of 0 counts only once the boss has HP at all
            this.death = () => module.PrimaryActor.IsDeadOrDestroyed || module.PrimaryActor.HPMP.MaxHP > 0 && module.PrimaryActor.HPMP.CurHP == 0;
            this.Raw.Update = this.death;
        }

        /// <summary>Run an action when this phase begins.</summary>
        public PhaseDef OnEnter(Action action, bool condition = true)
        {
            if (condition)
                this.Enter += action;
            return this;
        }

        /// <summary>Run an action when this phase ends.</summary>
        public PhaseDef OnExit(Action action, bool condition = true)
        {
            if (condition)
                this.Exit += action;
            return this;
        }

        /// <summary>Turn component <typeparamref name="T"/> on when this phase begins. The <paramref name="condition"/>
        /// overload lets a ported module gate a component on a difficulty or a config choice.</summary>
        public PhaseDef ActivateOnEnter<T>(bool condition = true) where T : ModuleComponent
        {
            if (condition)
            {
                this.EnterComponents.Add(typeof(T));
                this.Enter += module.ActivateComponent<T>;
            }
            return this;
        }

        public PhaseDef DeactivateOnEnter<T>(bool condition = true) where T : ModuleComponent => this.OnEnter(module.DeactivateComponent<T>, condition);

        /// <summary>Turn <typeparamref name="T"/> off when this phase ends: for a component that keeps across phase changes.</summary>
        public PhaseDef DeactivateOnExit<T>(bool condition = true) where T : ModuleComponent => this.OnExit(module.DeactivateComponent<T>, condition);

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
