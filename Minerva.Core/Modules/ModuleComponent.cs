namespace Minerva;

/// <summary>
/// A self-contained piece of encounter logic (one mechanic, usually). Components are activated and
/// deactivated by the state machine, observe world events, contribute text hints, and draw on the
/// radar. This is the unit modules are assembled from — most are one-liner subclasses of a
/// framework component like <c>SimpleAOEs</c>.
/// </summary>
public abstract class ModuleComponent(ModuleBase module)
{
    public readonly ModuleBase Module = module;

    public WorldState World => this.Module.World;
    public Arena Arena => this.Module.Arena;

    /// <summary>Per-actor advice lines; <c>risk</c> marks danger (rendered prominently).</summary>
    public sealed class TextHints : List<(string text, bool risk)>
    {
        public void Add(string text, bool risk = true) => this.Add((text, risk));
    }

    /// <summary>Raid-wide advice lines.</summary>
    public sealed class GlobalHints : List<string>;

    public virtual void Update() { }
    public virtual void AddHints(int slot, Actor actor, TextHints hints) { }
    public virtual void AddGlobalHints(GlobalHints hints) { }

    /// <summary>Contribute danger zones for the auto-dodge engine (see <see cref="AIHints"/>).</summary>
    public virtual void AddAIHints(int slot, Actor actor, AIHints hints) { }

    /// <summary>Draw danger zones / fills (called before the boundary).</summary>
    public virtual void DrawArenaBackground(int pcSlot, Actor pc) { }

    /// <summary>Draw actors / markers / tethers (called after the boundary).</summary>
    public virtual void DrawArenaForeground(int pcSlot, Actor pc) { }

    // world event handlers (dispatched by the module)
    public virtual void OnActorCreated(Actor actor) { }
    public virtual void OnActorDestroyed(Actor actor) { }
    public virtual void OnActorDeath(Actor actor) { }
    public virtual void OnCastStarted(Actor caster, ActorCastInfo cast) { }
    public virtual void OnCastFinished(Actor caster, ActorCastInfo cast) { }
    public virtual void OnEventCast(Actor caster, ActorCastEvent cast) { }
    // note: matches BMR's signatures so ported modules compile unchanged. On a status change the
    // struct is passed by ref; on lose it still carries the lost status's details (see OpStatus).
    public virtual void OnStatusGain(Actor actor, ref ActorStatus status) { }
    public virtual void OnStatusLose(Actor actor, ref ActorStatus status) { }
    public virtual void OnTethered(Actor source, in ActorTetherInfo tether) { }
    public virtual void OnUntethered(Actor source, in ActorTetherInfo tether) { }
    public virtual void OnMapEffect(byte index, uint state) { }
    public virtual void OnEventIcon(Actor actor, uint iconID, ulong targetID) { }
}
