namespace Minerva;

/// <summary>Timeline surface for ported modules — see <see cref="StateMachineBuilder.State"/>.</summary>
public abstract partial class StateMachineBuilder
{
    /// <summary>
    /// One state of a fight's timeline: BossmodReborn's <c>StateMachine.State</c> and its builder wrapper in one.
    ///
    /// <para>A BossmodReborn module describes a fight twice over. Its components say what each mechanic looks like;
    /// its states lay those mechanics out in order -- this cast, then that one -- and switch components on and off as
    /// the fight walks through them. Each state waits for something (a cast to start or end, a component's count, a
    /// timeout), runs its exit actions, and hands over to the next, whose enter actions run.</para>
    ///
    /// <para>Until 2026-10-17 Minerva recorded these and ran none of them: everything a state activated was on for
    /// the whole phase and nothing was ever switched off. That was thought the safe direction -- an idle component
    /// draws nothing -- and it is not, for a component that remembers. Another Mount Rokkon, read against Splatoon's
    /// presets: Gorai's cross forbidden from the pull, Moko's Oni's Claw never clearing, Unnatural Wail's targets
    /// piling up from one cast to the next. 3,372 deactivations across 108 module files were being dropped.</para>
    /// </summary>
    public sealed class State
    {
        private readonly ModuleBase module;

        internal State(ModuleBase module, PhaseDef phase, uint id, string name, float duration)
        {
            this.module = module;
            this.Phase = phase;
            this.ID = id;
            this.Name = name;
            this.Raw = new StateRaw(duration);
        }

        public readonly uint ID;
        public readonly string Name;

        /// <summary>Expected duration in seconds, as the module author declared it: what timeouts and overdue checks
        /// measure against, and what <see cref="StateMachine.NextTransitionWithFlag"/> adds up.</summary>
        public float Duration => this.Raw.Duration;

        /// <summary>Hint flags the module attached to this state's end (raidwide, tankbuster, knockback, downtime…).</summary>
        public StateMachine.StateHint Hints { get; private set; }

        /// <summary>BossmodReborn's <c>state.Raw</c>: the hooks a ported states file sets directly.</summary>
        public StateRaw Raw { get; }

        /// <summary>The phase this state belongs to.</summary>
        public PhaseDef Phase { get; }

        /// <summary>The states this one can hand over to; <see cref="StateRaw.Update"/> returns an index into it.
        /// One for a plain chain, one per branch for a fork, none at the end of a chain.</summary>
        public State[]? NextStates { get; internal set; }

        internal Action? Enter;
        internal Action? Exit;

        /// <summary>Per-state hooks, mirroring BossmodReborn's <c>state.Raw</c>.</summary>
        public sealed class StateRaw(float duration)
        {
            /// <summary>The module author's note about what this state is. Purely documentary.</summary>
            public string Comment = "";

            /// <summary>Expected duration in seconds.</summary>
            public float Duration = duration;

            /// <summary>Given seconds since the state began, the index in <see cref="NextStates"/> to move to, or -1 to
            /// stay. Called every frame while the state is active.</summary>
            public Func<float, int>? Update;
        }

        public State OnEnter(Action action, bool condition = true)
        {
            if (condition)
                this.Enter += action;
            return this;
        }

        /// <summary>Runs when the state hands over, and also when its phase ends under it.</summary>
        public State OnExit(Action action, bool condition = true)
        {
            if (condition)
                this.Exit += action;
            return this;
        }

        public State ActivateOnEnter<T>(bool condition = true) where T : ModuleComponent => this.OnEnter(this.module.ActivateComponent<T>, condition);
        public State ActivateOnExit<T>(bool condition = true) where T : ModuleComponent => this.OnExit(this.module.ActivateComponent<T>, condition);
        public State DeactivateOnEnter<T>(bool condition = true) where T : ModuleComponent => this.OnEnter(this.module.DeactivateComponent<T>, condition);
        public State DeactivateOnExit<T>(bool condition = true) where T : ModuleComponent => this.OnExit(this.module.DeactivateComponent<T>, condition);
        public State ExecOnEnter<T>(Action<T> fn, bool condition = true) where T : ModuleComponent => this.OnEnter(this.ExecFor(fn), condition);
        public State ExecOnExit<T>(Action<T> fn, bool condition = true) where T : ModuleComponent => this.OnExit(this.ExecFor(fn), condition);

        /// <summary>A fresh instance of the component when this state hands over, so the next round of the mechanic
        /// starts from nothing (its cast counts, its targets).</summary>
        public State ResetComp<T>(bool condition = true) where T : ModuleComponent => this.OnExit(() =>
        {
            this.module.DeactivateComponent<T>();
            this.module.ActivateComponent<T>();
        }, condition);

        public State SetHint(StateMachine.StateHint h, bool condition = true)
        {
            if (condition)
                this.Hints |= h;
            return this;
        }

        public State ClearHint(StateMachine.StateHint h, bool condition = true)
        {
            if (condition)
                this.Hints &= ~h;
            return this;
        }

        private Action ExecFor<T>(Action<T> fn) where T : ModuleComponent => () =>
        {
            if (this.module.FindComponent<T>() is { } c)
                fn(c);
            else
                this.module.ReportError($"Component {typeof(T).Name} needed for state {this.ID:X} was not found");
        };
    }
}

/// <summary>
/// The running state machine: BossmodReborn's, run the way BossmodReborn runs it.
///
/// <para>Phases in order, each ending when its transition holds; within a phase, a walk from its initial state along
/// whatever each state's update chooses. A phase change ends the active state (its exit actions run), runs the phase's
/// own exit actions, and clears every component that does not ask to keep across phases
/// (<see cref="ModuleComponent.KeepOnPhaseChange"/>), as BossmodReborn's <c>SimplePhase</c> does.</para>
///
/// <para>When it starts: a module whose states file declares states starts on the pull, as in BossmodReborn, so a
/// timeline's timeouts count from the fight and not from the boss being loaded. A module with no states -- one phase
/// that switches components on, Minerva's own modules and most small ports -- starts the moment it is built, as it
/// always has.</para>
/// </summary>
public sealed class StateMachine(ModuleBase module)
{
    private IReadOnlyList<StateMachineBuilder.PhaseDef> phases = [];

    /// <summary>Zero-based index of the phase running now, or -1 before the machine starts.</summary>
    public int ActivePhaseIndex { get; private set; } = -1;

    public StateMachineBuilder.PhaseDef? ActivePhase => this.ActivePhaseIndex >= 0 && this.ActivePhaseIndex < this.phases.Count ? this.phases[this.ActivePhaseIndex] : null;

    /// <summary>The state the fight is in, or null before the machine starts or in a phase with no states.</summary>
    public StateMachineBuilder.State? ActiveState { get; private set; }

    /// <summary>When <see cref="ActiveState"/> began.</summary>
    public DateTime LastTransition { get; private set; }

    /// <summary>When the current phase began; <c>default</c> before the machine starts.</summary>
    public DateTime PhaseEnteredAt { get; private set; }

    /// <summary>Has the machine been started (<see cref="Start"/>)?</summary>
    public bool Started { get; private set; }

    /// <summary>The last phase's own end has held: the module says its fight is over.</summary>
    public bool Finished { get; private set; }

    /// <summary>
    /// Seconds since the current phase began, or 0 before the machine starts. Phase time, not state time: a module
    /// uses it to hold off at the start of a phase, which no cast or state transition says.
    /// </summary>
    public float TimeSincePhaseEnter
        => this.PhaseEnteredAt == default ? 0f : (float)(module.World.CurrentTime - this.PhaseEnteredAt).TotalSeconds;

    /// <summary>Seconds since <see cref="ActiveState"/> began.</summary>
    public float TimeSinceTransition => (float)(module.World.CurrentTime - this.LastTransition).TotalSeconds;

    /// <summary>
    /// When the next state whose ending carries <paramref name="flag"/> will end -- "when is the next raidwide",
    /// answered off the module author's declared durations from the active state forward. <see cref="DateTime.MaxValue"/>
    /// when it cannot say: nothing active, a fork in the way, or the end of the chain. BossmodReborn's contract exactly.
    /// </summary>
    public DateTime NextTransitionWithFlag(StateHint flag)
    {
        var time = this.LastTransition;
        var next = this.ActiveState;
        while (next != null)
        {
            time = time.AddSeconds(next.Duration);
            if ((next.Hints & flag) != 0)
                return time;
            next = next.NextStates?.Length == 1 ? next.NextStates[0] : null;
        }
        return DateTime.MaxValue;
    }

    /// <summary>Enter the first phase and its initial state.</summary>
    internal void Start(IReadOnlyList<StateMachineBuilder.PhaseDef> declared)
    {
        this.phases = declared;
        this.Started = true;
        if (declared.Count != 0)
            this.TransitionToPhase(0);
    }

    /// <summary>One frame: phases advance while their transitions hold, then the state walk moves on as far as it goes.</summary>
    internal void Update()
    {
        for (var guard = 0; guard < 16 && this.ActivePhase is { } phase; ++guard)
        {
            if (phase.Transition is not { } transition || !transition())
                break;
            if (this.ActivePhaseIndex == this.phases.Count - 1)
            {
                // the last phase's end is the fight's; a death end is the boss's lifetime, which the module manager has
                if (!phase.EndsOnDeath)
                    this.Finished = true;
                break;
            }
            this.TransitionToPhase(this.ActivePhaseIndex + 1);
        }

        for (var guard = 0; guard < 64 && this.ActiveState is { } state; ++guard)
        {
            var index = state.Raw.Update?.Invoke(this.TimeSinceTransition) ?? -1;
            if (state.NextStates is not { } next || index < 0 || index >= next.Length || next[index] is not { } target)
                break;
            this.TransitionToState(target);
        }
    }

    private void TransitionToPhase(int index)
    {
        if (this.ActivePhase is { } old)
        {
            this.TransitionToState(null);
            old.Exit?.Invoke();
            module.ClearComponents(c => !c.KeepOnPhaseChange);
        }

        this.ActivePhaseIndex = index;
        this.PhaseEnteredAt = module.World.CurrentTime;
        if (this.ActivePhase is { } entered)
        {
            entered.Enter?.Invoke();
            this.TransitionToState(entered.InitialState);
        }
    }

    private void TransitionToState(StateMachineBuilder.State? next)
    {
        this.ActiveState?.Exit?.Invoke();
        this.ActiveState = next;
        this.LastTransition = module.World.CurrentTime;
        next?.Enter?.Invoke();
    }

    /// <summary>What a phase's beginning means. Recorded, not acted on.</summary>
    [Flags]
    public enum PhaseHint
    {
        None = 0,

        /// <summary>The phase opens with the boss untargetable, so uptime advice does not apply yet.</summary>
        StartWithDowntime = 1 << 0,
    }

    /// <summary>
    /// What a state's ending means, mirroring BossmodReborn's <c>StateMachine.StateHint</c>. Read by
    /// <see cref="NextTransitionWithFlag"/>; otherwise recorded, and several of these are things Minerva already
    /// derives from components instead.
    /// </summary>
    [Flags]
    public enum StateHint
    {
        None = 0,
        GroupWithNext = 1 << 0,
        BossCastStart = 1 << 1,
        BossCastEnd = 1 << 2,
        Tankbuster = 1 << 3,
        Raidwide = 1 << 4,
        Knockback = 1 << 5,
        DowntimeStart = 1 << 6,
        DowntimeEnd = 1 << 7,
        PositioningStart = 1 << 8,
        PositioningEnd = 1 << 9,
        VulnerableStart = 1 << 10,
        VulnerableEnd = 1 << 11,
    }
}
