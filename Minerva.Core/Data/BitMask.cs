namespace Minerva;

/// <summary>
/// A 64-bit set of slot indices (party members, actor slots), matching BossmodReborn's <c>BitMask</c> so
/// ported modules and future party-targeted mechanics can use it directly. Thin wrapper over a ulong.
/// </summary>
public struct BitMask(ulong raw = 0)
{
    public ulong Raw = raw;

    /// <summary>A method, not a property, because BossmodReborn's is — and every ported module calls it
    /// that way. Nothing in Minerva used it as a property.</summary>
    public readonly bool None() => this.Raw == 0;
    /// <summary>Whether any bit is set. A method, not a property, so ported code's <c>mask.Any()</c>
    /// binds here instead of falling through to LINQ's extension and failing to infer.</summary>
    public readonly bool Any() => this.Raw != 0;
    /// <summary>How many bits are set. A method, not a property, because that is how ported modules
    /// call it and a name cannot be both.</summary>
    public readonly int NumSetBits() => System.Numerics.BitOperations.PopCount(this.Raw);

    /// <summary>Settable, because ported modules assign to it directly (<c>mask[slot] = true</c>).</summary>
    public bool this[int index]
    {
        readonly get => (this.Raw & (1ul << index)) != 0;
        set
        {
            if (value)
                this.Raw |= 1ul << index;
            else
                this.Raw &= ~(1ul << index);
        }
    }

    public void Set(int index) => this.Raw |= 1ul << index;
    public void Clear(int index) => this.Raw &= ~(1ul << index);
    public void Toggle(int index) => this.Raw ^= 1ul << index;

    /// <summary>Fluent set: returns a copy with <paramref name="index"/> set (BMR's <c>WithBit</c>).</summary>
    public readonly BitMask WithBit(int index) => index >= 0 ? new BitMask(this.Raw | (1ul << index)) : this;

    /// <summary>The mask with one bit cleared. Modules use it to ask "who else" — drop myself, then take
    /// the lowest remaining bit to find my partner.</summary>
    public readonly BitMask WithoutBit(int index) => index >= 0 ? new BitMask(this.Raw & ~(1ul << index)) : this;

    public readonly int LowestSetBit() => this.Raw == 0 ? -1 : System.Numerics.BitOperations.TrailingZeroCount(this.Raw);

    public readonly int HighestSetBit() => this.Raw == 0 ? -1 : 63 - System.Numerics.BitOperations.LeadingZeroCount(this.Raw);

    /// <summary>Clear every bit. Components call this between mechanic repeats to forget who was hit.</summary>
    public void Reset() => this.Raw = 0;

    public static BitMask Build(params int[] bits)
    {
        var m = default(BitMask);
        foreach (var b in bits)
            m.Set(b);
        return m;
    }

    /// <summary>Shift the whole mask. Modules fold an 8-slot mask onto a 4-slot one with <c>m | (m >> 4)</c>.</summary>
    public static BitMask operator <<(BitMask a, int bits) => new(a.Raw << bits);
    public static BitMask operator >>(BitMask a, int bits) => new(a.Raw >> bits);

    public static BitMask operator &(BitMask a, BitMask b) => new(a.Raw & b.Raw);
    public static BitMask operator |(BitMask a, BitMask b) => new(a.Raw | b.Raw);
    public static BitMask operator ^(BitMask a, BitMask b) => new(a.Raw ^ b.Raw);
    public static BitMask operator ~(BitMask a) => new(~a.Raw);
    public static bool operator ==(BitMask a, BitMask b) => a.Raw == b.Raw;
    public static bool operator !=(BitMask a, BitMask b) => a.Raw != b.Raw;

    public readonly bool Equals(BitMask other) => this.Raw == other.Raw;
    public override readonly bool Equals(object? obj) => obj is BitMask other && this.Raw == other.Raw;
    public override readonly int GetHashCode() => this.Raw.GetHashCode();
    public override readonly string ToString() => $"0x{this.Raw:X}";

    /// <summary>The indices that are set, ascending. Ported components enumerate marked players with it.</summary>
    public readonly int[] SetBits()
    {
        var n = this.NumSetBits();
        if (n == 0)
            return [];
        var result = new int[n];
        var at = 0;
        for (var i = 0; i < 64 && at < n; ++i)
            if (this[i])
                result[at++] = i;
        return result;
    }

}
