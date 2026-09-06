namespace Minerva;

/// <summary>Timeline surface for ported modules — see <see cref="StateMachineBuilder.State"/>.</summary>
public abstract partial class StateMachineBuilder
{
    /// <summary>
    /// The timeline half of BossmodReborn's state machine, as far as Minerva needs it.
    ///
    /// <para><b>What this is for.</b> A BossmodReborn module describes a fight twice over. Its
    /// <i>components</i> say what each mechanic looks like and where it is dangerous; its <i>states</i> lay
    /// those mechanics out on a timeline — this cast, then eight seconds, then that cast — and switch
    /// components on and off as the fight walks through it. Minerva needs the first half and uses almost none
    /// of the second: it has no timeline display, no plan editor, and no cooldown planner to feed. Its own
    /// machine is phases only, and a phase just activates a set of components.</para>
    ///
    /// <para><b>Why it exists anyway.</b> Fifty-three ported modules keep their whole component list inside
    /// state builders, so without this surface they do not compile — 101 errors, the single largest blocker in
    /// the port. The mechanics are all there; only the scaffolding is missing.</para>
    ///
    /// <para><b>What it actually does.</b> Component activation is honoured for real: anything a state
    /// activates is activated on the phase that contains it. Timing is not — durations, cast triggers and
    /// timeouts are recorded and never drive anything. So a component BossmodReborn would switch on for six
    /// seconds is, here, on for the whole phase.</para>
    ///
    /// <para><b>Why that is the safe direction.</b> An over-active component draws a zone that is not live
    /// yet; it never hides one that is. Every component already gates its own drawing on a live cast or a live
    /// actor, so in practice an idle one draws nothing at all. The failure mode is a spurious warning, not a
    /// missed mechanic — and a spurious warning is a bug you can see, which is the kind worth having.</para>
    ///
    /// <para>Names, argument order and defaults match BossmodReborn exactly so ported files need no edits. The
    /// ids and durations are kept rather than dropped, because a real timeline would need them and throwing
    /// them away would mean re-porting fifty-three files to get them back.</para>
    /// </summary>
    public sealed class State(ModuleBase module, PhaseDef phase, uint id, string name, float duration)
    {
        public readonly uint ID = id;
        public readonly string Name = name;
    
        /// <summary>Expected duration in seconds, as the module author declared it. Recorded, not enforced.</summary>
        public readonly float Duration = duration;
    
        /// <summary>Hint flags the module attached (raidwide, tankbuster, knockback, downtime…).</summary>
        public StateMachine.StateHint Hints { get; private set; }

        /// <summary>
        /// Action ids whose cast start means this state has begun; empty for a state driven by a timeout or
        /// a predicate, which Minerva has no way to observe.
        /// </summary>
        public uint[] TriggerAIDs { get; internal set; } = [];

        /// <summary>
        /// This state branches — the fight can go several ways from here. The timeline walk stops at one,
        /// exactly as BossmodReborn's does, because past a fork the declared order is one possible future
        /// rather than the future.
        /// </summary>
        public bool IsBranch { get; internal set; }

        /// <summary>
        /// Raw per-state hooks, mirroring BossmodReborn's <c>state.Raw</c>. Distinct from
        /// <see cref="StateMachineBuilder.PhaseDef.Raw"/>, which is phase-level and shaped differently: a
        /// phase's <c>Update</c> answers "advance yet?", a state's answers "which state next?" given the time
        /// since it began. Recorded, not run — Minerva's machine is phases only.
        /// </summary>
        public StateRaw Raw { get; } = new(duration);

        /// <summary>Per-state hooks BossmodReborn's timeline would drive. See <see cref="Raw"/>.</summary>
        public sealed class StateRaw(float duration)
        {
            /// <summary>The module author's note about what this state is. Purely documentary.</summary>
            public string Comment = "";

            /// <summary>Expected duration in seconds, as declared.</summary>
            public float Duration = duration;

            /// <summary>Given seconds since the state began, the index of the state to move to, or -1 to
            /// stay. Recorded and never called.</summary>
            public Func<float, int>? Update;
        }
    
        /// <summary>The phase this state belongs to. Component activation lands here.</summary>
        public PhaseDef Phase => phase;
    
        public State ActivateOnEnter<T>(bool condition = true) where T : ModuleComponent
        {
            if (condition)
                phase.ActivateOnEnter<T>();
            return this;
        }
    
        /// <summary>Same as <see cref="ActivateOnEnter{T}"/> here: without a timeline, enter and exit coincide.</summary>
        public State ActivateOnExit<T>(bool condition = true) where T : ModuleComponent => this.ActivateOnEnter<T>(condition);
    
        /// <summary>
        /// Deactivation is deliberately ignored.
        /// <para>With no timeline there is no moment to deactivate at, and honouring it at phase level would
        /// switch a component off for the whole phase — turning a mechanic that fires later in the fight into
        /// one that is never drawn. Leaving it on is the recoverable mistake.</para>
        /// </summary>
        public State DeactivateOnEnter<T>(bool condition = true) where T : ModuleComponent => this;
    
        public State DeactivateOnExit<T>(bool condition = true) where T : ModuleComponent => this;
    
        public State OnEnter(Action action, bool condition = true)
        {
            if (condition)
                phase.OnEnter(action);
            return this;
        }
    
        /// <summary>Runs on phase entry, as above — there is no exit instant to hang it on.</summary>
        public State OnExit(Action action, bool condition = true) => this.OnEnter(action, condition);
    
        public State ExecOnEnter<T>(Action<T> fn, bool condition = true) where T : ModuleComponent
            => this.OnEnter(() =>
            {
                if (module.FindComponent<T>() is { } c)
                    fn(c);
            }, condition);
    
        public State ExecOnExit<T>(Action<T> fn, bool condition = true) where T : ModuleComponent => this.ExecOnEnter(fn, condition);
    
        public State ResetComp<T>(bool condition = true) where T : ModuleComponent => this;
    
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
    }
    
}

/// <summary>
/// The running state machine, as far as ported modules address it: the current phase index, plus the
/// vocabulary BossmodReborn's machine uses, spelled the way ported files spell it
/// (<c>StateMachine.StateHint.Raidwide</c>). Minerva records the hints and acts on none of them — several
/// are things it already derives from components instead.
/// </summary>
public sealed class StateMachine(ModuleBase module)
{
    /// <summary>
    /// Zero-based index of the phase running now, or -1 before the machine starts. BossmodReborn's name for
    /// <see cref="ModuleBase.CurrentPhase"/>; modules switch on it to draw a different phase's enemies.
    /// </summary>
    public int ActivePhaseIndex => module.CurrentPhase;

    /// <summary>
    /// Seconds since the current phase began, or 0 before the machine starts.
    ///
    /// <para>Phase time, not state time: a module uses it to hold off at the start of a phase — "do nothing
    /// for a few seconds so the tank can position the boss" — which no cast or state transition says.</para>
    /// </summary>
    public float TimeSincePhaseEnter
        => module.PhaseEnteredAt == default ? 0f : (float)(module.World.CurrentTime - module.PhaseEnteredAt).TotalSeconds;

    /// <summary>
    /// The state the fight is in, or null before any tracked cast has been seen.
    ///
    /// <para>Tracked by observing casts, not by running the machine: when an actor begins casting an action
    /// a state names as its trigger, that state becomes current. States driven by a timeout or a predicate
    /// cannot be observed, so the pointer stays on the last cast-triggered state and the walk below carries
    /// on through the declared durations from there — which is what those durations are for.</para>
    /// </summary>
    public StateMachineBuilder.State? ActiveState { get; private set; }

    /// <summary>When <see cref="ActiveState"/> began. The anchor the timeline estimate is measured from.</summary>
    public DateTime LastTransition { get; private set; }

    /// <summary>
    /// When the next state whose ending carries <paramref name="flag"/> will end —
    /// "when is the next raidwide", answered off the module author's own timings.
    ///
    /// <para>Walks forward from <see cref="ActiveState"/> summing declared durations, and returns
    /// <see cref="DateTime.MaxValue"/> when it cannot say: no state tracked yet, a branch in the way, or the
    /// end of the timeline. That is BossmodReborn's contract exactly, including the MaxValue — the components
    /// asking this are written against it, and a fight that has gone off-script should say "I don't know"
    /// rather than invent a time.</para>
    ///
    /// <para>The number is an ESTIMATE from declared durations, not an observation. It is exactly as good as
    /// the module author's timings, which is also true of BossmodReborn's answer.</para>
    /// </summary>
    public DateTime NextTransitionWithFlag(StateHint flag)
    {
        var states = module.Timeline;
        var current = this.ActiveState;
        if (current == null || states.Count == 0)
            return DateTime.MaxValue;

        var i = -1;
        for (var k = 0; k < states.Count; ++k)
        {
            if (ReferenceEquals(states[k], current))
            {
                i = k;
                break;
            }
        }

        if (i < 0)
            return DateTime.MaxValue;

        var time = this.LastTransition;
        for (; i < states.Count; ++i)
        {
            var s = states[i];
            time = time.AddSeconds(s.Duration);
            if ((s.Hints & flag) != 0)
                return time;
            if (s.IsBranch)
                break; // past a fork the declared order is one possible future, not the future
        }

        return DateTime.MaxValue;
    }

    /// <summary>Advance the pointer when a cast a state names as its trigger begins.</summary>
    internal void NotifyCastStarted(uint aid, DateTime now)
    {
        foreach (var s in module.Timeline)
        {
            var triggers = s.TriggerAIDs;
            for (var i = 0; i < triggers.Length; ++i)
            {
                if (triggers[i] == aid)
                {
                    this.ActiveState = s;
                    this.LastTransition = now;
                    return;
                }
            }
        }
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
    /// What a state's ending means, mirroring BossmodReborn's <c>StateMachine.StateHint</c>.
    /// <para>Recorded rather than acted on. It is the vocabulary a timeline would need — "this one is a
    /// raidwide", "this one is a knockback" — and several of these are things Minerva already derives from
    /// components instead.</para>
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
