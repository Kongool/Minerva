namespace Minerva.Generation;

/// <summary>An object id observed in the fight, with lifetime + voidzone candidacy.</summary>
public readonly record struct ObjectFact(
    uint OID, string Name, float HitboxRadius, int Count, int Casts,
    double LifetimeSeconds, bool VoidzoneCandidate);

/// <summary>How a cast is aimed — decides which component fits.</summary>
public enum TargetKind
{
    Self,     // boss/helper -> self (fixed-position AOE at caster)
    Location, // -> ground location
    Player,   // -> a player (tankbuster / spread bait)
    Target,   // -> some other actor
}

/// <summary>What the correlation layer concluded a player-targeted cast is doing.</summary>
public enum PlayerMechanic
{
    None,        // not player-targeted
    Spread,      // many players hit at once / rotating targets
    Stack,       // pile-in marker
    Tankbuster,  // repeatedly the same (tank) target
    Bait,        // tethered/marked single-target bait
}

/// <summary>
/// An action observed in the fight, with the correlated facts the generator needs: base cast info,
/// how it's aimed, distinct/simultaneous player targets, and any icon/tether seen just before it.
/// </summary>
public readonly record struct ActionFact(
    uint AID, uint CasterOID, string CasterName, TargetKind Target, float CastTime, int Count,
    int DistinctPlayerTargets, int MaxSimultaneous, PlayerMechanic PlayerMechanic,
    uint PrecedingIcon, uint PrecedingTether, int Phase, bool ExaflareCandidate = false,
    bool ConcentricCandidate = false, bool GazeCandidate = false, float KnockbackDistance = 0f);

/// <summary>What kind of signal starts a phase — decides which transition the generator emits.</summary>
public enum PhaseTrigger
{
    Targetable, // a (new) boss form became targetable
    PrimaryHP,  // the boss reached an HP threshold (went untargetable there and later returned)
    MapEffect,  // a one-shot arena change (ENVC map effect) fired
}

/// <summary>
/// A detected phase of the fight. Depending on <see cref="Trigger"/>: <see cref="TriggerOID"/> is the
/// boss form whose appearance started a Targetable phase; <see cref="TriggerHP"/> (0..1) is the HP the
/// boss disengaged at for a PrimaryHP phase; <see cref="TriggerMapIndex"/>/<see cref="TriggerMapState"/>
/// identify the arena change for a MapEffect phase. The generator emits the matching transition.
/// </summary>
public readonly record struct PhaseFact(
    int Index, string Name, double StartSeconds, uint TriggerOID,
    PhaseTrigger Trigger = PhaseTrigger.Targetable, float TriggerHP = 0f,
    byte TriggerMapIndex = 0, uint TriggerMapState = 0);

/// <summary>Rough arena geometry mined from cast/actor positions.</summary>
public readonly record struct ArenaEstimate(WPos Center, float MinX, float MaxX, float MinZ, float MaxZ)
{
    public float HalfExtent => MathF.Max(MathF.Max(this.MaxX - this.Center.X, this.Center.X - this.MinX),
                                         MathF.Max(this.MaxZ - this.Center.Z, this.Center.Z - this.MinZ));
    public bool LooksSquare => MathF.Abs((this.MaxX - this.MinX) - (this.MaxZ - this.MinZ)) < 4f;
}

/// <summary>
/// Everything the module generator needs, mined and correlated from a replay: the duty key, the
/// likely boss, the objects and actions seen (with mechanic classification), phases, and an arena
/// estimate. Produced by <c>ReplayAnalysis.BuildGenerationInput</c>.
/// </summary>
public sealed class GenerationInput
{
    public required ushort Zone { get; init; }
    public required ushort CFCID { get; init; }
    public required uint BossOID { get; init; }
    public required string BossName { get; init; }
    public required IReadOnlyList<ObjectFact> Objects { get; init; }
    public required IReadOnlyList<ActionFact> Actions { get; init; }
    public required ArenaEstimate Arena { get; init; }
    public IReadOnlyList<PhaseFact> Phases { get; init; } = [];
    public IReadOnlyList<uint> Statuses { get; init; } = [];
    public IReadOnlyList<uint> Tethers { get; init; } = [];
    public IReadOnlyList<uint> Icons { get; init; } = [];
}
