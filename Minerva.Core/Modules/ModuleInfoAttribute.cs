namespace Minerva;

/// <summary>How finished/trustworthy a module is.</summary>
public enum ModuleMaturity
{
    WIP,        // under construction — do not trust
    Verified,   // tested end-to-end in an actual duty
}

/// <summary>
/// What kind of content a module belongs to, and therefore which sheet its ids mean something in.
/// <para>Without this every Occult Crescent module collapses into one pile, because they all report the
/// same ContentFinderCondition: the zone. BossmodReborn keeps them apart with this, and the enum comments
/// are the whole trick — a critical engagement's name id is a <c>DynamicEvent</c> row while a foray FATE's
/// is a <c>Fate</c> row, so "is this a CE or a FATE?" is answerable from data rather than from the name.</para>
/// </summary>
public enum ModuleGroup
{
    /// <summary>Group id is a ContentFinderCondition row — ordinary instanced duties.</summary>
    CFC,

    /// <summary>Group id is a ContentFinderCondition row, name id is a <b>DynamicEvent</b> row.</summary>
    CriticalEngagement,

    /// <summary>Group id and name id are <b>Fate</b> rows.</summary>
    ForayFATE,

    /// <summary>An open-world FATE outside a foray zone. Ids are Fate rows.</summary>
    Fate,

    /// <summary>Group id is a HuntRank.</summary>
    Hunt,

    /// <summary>Group id is a Quest row.</summary>
    Quest,

    TheForkedTowerBlood,
    TheForkedTowerMagic,

    /// <summary>An Unreal trial that has since rotated out of the game. The module is kept because the
    /// rotation brings them back, and because a recording of one is still a recording.</summary>
    RemovedUnreal,
    BaldesionArsenal,
    CastrumLacusLitore,
    TheDalriada,
    BozjaDuel,
    EurekaNM,
    MaskedCarnivale,
    GoldSaucer,
    Other,
}

/// <summary>
/// Marks a <see cref="ModuleBase"/> subclass as a discoverable encounter module and carries the
/// keys the registry matches against the live game: the duty's Content Finder Condition id and the
/// boss's object id. Applied directly above the module class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleInfoAttribute : Attribute
{
    /// <summary>Which content family this belongs to — what separates a critical engagement from a FATE
    /// when both report the same duty. See <see cref="ModuleGroup"/>.</summary>
    public ModuleGroup Group { get; set; } = ModuleGroup.CFC;

    /// <summary>The id within <see cref="Group"/>'s own space, when that is not the CFC.</summary>
    public uint GroupID { get; set; }

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
