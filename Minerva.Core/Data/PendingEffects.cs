namespace Minerva;

/// <summary>
/// What one entry of a resolved action's effect payload does. Only the values Minerva reads are named;
/// the game defines many more. Matches BossmodReborn's numbering, which matches the game's.
/// </summary>
public enum ActionEffectType : byte
{
    Nothing = 0,
    Miss = 1,
    FullResist = 2,
    Damage = 3,
    Heal = 4,
    BlockedDamage = 5,
    ParriedDamage = 6,
    Invulnerable = 7,
    NoEffectText = 8,
    FailMissingStatus = 9,
    MpLoss = 10,
    MpGain = 11,
    TpLoss = 12,
    TpGain = 13,
    ApplyStatusEffectTarget = 14,
    ApplyStatusEffectSource = 15,
    RecoveredFromStatusEffect = 16,
    LoseStatusEffectTarget = 17,
    LoseStatusEffectSource = 18,
    StatusNoEffect = 20,
    Knockback = 31,
}

/// <summary>
/// One slot of an action's effect payload, decoded from the eight bytes the game sends.
///
/// <para>Minerva stores the payload as raw <c>ulong</c>s because that is how it arrives and because
/// storing it verbatim is what lets a recording be replayed years later against code that understands
/// more of it than the code that captured it did. This is the reader.</para>
/// </summary>
public readonly struct ActionEffect(ulong raw)
{
    public readonly ulong Raw = raw;

    public ActionEffectType Type => (ActionEffectType)(byte)this.Raw;
    public byte Param0 => (byte)(this.Raw >> 8);
    public byte Param1 => (byte)(this.Raw >> 16);
    public byte Param2 => (byte)(this.Raw >> 24);
    public byte Param3 => (byte)(this.Raw >> 32);
    public byte Param4 => (byte)(this.Raw >> 40);
    public ushort Value => (ushort)(this.Raw >> 48);

    /// <summary>The effect lands on the caster rather than on the target — a self-buff on a targeted action.</summary>
    public bool AtSource => (this.Param4 & 0x80) != 0;

    /// <summary>Damage or healing, in HP. The high bit of <see cref="Param4"/> extends the 16-bit value,
    /// which is how anything above 65535 is expressed.</summary>
    public int DamageHealValue => this.Value + ((this.Param4 & 0x40) != 0 ? this.Param3 * 0x10000 : 0);
}

/// <summary>A pending change to a resource, signed: damage is negative, healing positive.</summary>
public readonly record struct PendingEffectDelta(PendingEffect Effect, int Value);

/// <summary>A pending status application.</summary>
public readonly record struct PendingEffectStatus(PendingEffect Effect, uint StatusId);

/// <summary>A pending status application that also carries the low byte of the status extra, which is how
/// stack counts arrive. Split from <see cref="PendingEffectStatus"/> rather than folded into it because
/// BossmodReborn splits them, and a ported module names whichever one it expects.</summary>
public readonly record struct PendingEffectStatusExtra(PendingEffect Effect, uint StatusId, byte ExtraLo);
