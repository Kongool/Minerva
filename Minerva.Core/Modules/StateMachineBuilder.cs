namespace Minerva;

/// <summary>
/// Declares a module's phases: which components each phase turns on, and the condition that advances
/// to the next one. A concrete boss pairs with a <c>&lt;Name&gt;States : StateMachineBuilder</c> whose
/// constructor chains <c>Phase(...).ActivateOnEnter&lt;T&gt;().TransitionOn...(...)</c>. Phases run at
/// runtime: <see cref="ModuleBase"/> enters phase 0 when built and, each frame, advances to the next
/// phase once the current phase's transition fires (activating/deactivating components as it goes).
/// A single <see cref="TrivialPhase"/> with no transition reproduces the old "everything on" behaviour.
/// </summary>
public abstract class StateMachineBuilder(ModuleBase module)
{
    protected readonly ModuleBase Module = module;

    private readonly List<PhaseDef> phases = [];

    /// <summary>The declared phases, in order. Read by <see cref="ModuleBase"/> to run the machine.</summary>
    public IReadOnlyList<PhaseDef> Phases => this.phases;

    /// <summary>A single phase that stays on for the whole encounter (no transition out).</summary>
    protected PhaseDef TrivialPhase() => this.Phase("Main");

    // convenience predicates for phase transitions (delegate to the module), matching BMR usage
    protected bool AllDeadOrDestroyed(uint oid) => this.Module.AllDeadOrDestroyed(oid);
    protected bool AllDeadOrDestroyed(uint[] oids) => this.Module.AllDeadOrDestroyed(oids);
    protected bool AllDeadOrDestroyedInBounds(uint oid) => this.Module.AllDeadOrDestroyedInBounds(oid);
    protected bool AllDeadOrDestroyedInBounds(uint[] oids) => this.Module.AllDeadOrDestroyedInBounds(oids);

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
        public PhaseDef ActivateOnEnter<T>() where T : ModuleComponent
        {
            this.EnterComponents.Add(typeof(T));
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
