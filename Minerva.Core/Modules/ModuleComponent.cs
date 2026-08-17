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

    /// <summary>The player's party — matches BMR's <c>Raid</c> accessor so ported components using
    /// <c>Raid.WithSlot()</c> / <c>Raid.Player()</c> / <c>Raid.FindSlot()</c> compile unchanged.</summary>
    public PartyState Raid => this.Module.World.Party;

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

    /// <summary>Contribute danger zones for the auto-dodge engine (see <see cref="AIHints"/>). The
    /// <paramref name="assignment"/> role slot (matching BMR) lets role-based positioning port unchanged.</summary>
    public virtual void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints) { }

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

    // Additional BMR event hooks, declared so ported components that override them compile. Minerva's
    // world sync does not raise all of these yet, so overrides may not fire until the sync is extended
    // (tracked in the BMR-porting notes); they are safe no-ops until then.
    public virtual void OnActorTargetable(Actor actor) { }
    public virtual void OnActorUntargetable(Actor actor) { }
    public virtual void OnActorRenderflagsChange(Actor actor, int renderflags) { }
    public virtual void OnEventVFX(Actor actor, uint vfxID, ulong targetID) { }
    public virtual void OnActorEState(Actor actor, ushort state) { }
    public virtual void OnActorEAnim(Actor actor, uint state) { }
    public virtual void OnActorPlayActionTimelineEvent(Actor actor, ushort id) { }
    public virtual void OnActorNpcYell(Actor actor, ushort id) { }
    public virtual void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2) { }
    public virtual void OnActorEventStateChange(Actor actor, byte value) { }
    public virtual void OnLegacyMapEffect(byte seq, byte param, byte[] data) { }
    public virtual void OnEventDirectorUpdate(uint updateID, uint param1, uint param2, uint param3, uint param4) { }
}
