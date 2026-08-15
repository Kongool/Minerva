namespace Minerva;

/// <summary>How finished/trustworthy a module is.</summary>
public enum ModuleMaturity
{
    WIP,        // under construction — do not trust
    Verified,   // tested end-to-end in an actual duty
}

/// <summary>
/// Marks a <see cref="ModuleBase"/> subclass as a discoverable encounter module and carries the
/// keys the registry matches against the live game: the duty's Content Finder Condition id and the
/// boss's object id. Applied directly above the module class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleInfoAttribute : Attribute
{
    /// <summary>Content Finder Condition id of the duty this module belongs to.</summary>
    public uint CFCID { get; set; }

    /// <summary>Primary/boss actor OID that triggers this module (0 = infer from an <c>OID.Boss</c> enum member).</summary>
    public uint PrimaryActorOID { get; set; }

    /// <summary>BNpcName row id (for display).</summary>
    public uint NameID { get; set; }

    /// <summary>
    /// Whether death of this module's primary actor positively completes the encounter. Keep false
    /// for multi-form or multi-boss encounters whose primary actor can die before the fight is over.
    /// </summary>
    public bool PrimaryActorDeathEndsEncounter { get; set; }

    public ModuleMaturity Maturity { get; set; } = ModuleMaturity.WIP;
    public string Contributors { get; set; } = "";
}
