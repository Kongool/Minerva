namespace Minerva;

/// <summary>
/// A 64-bit set of slot indices (party members, actor slots), matching BossmodReborn's <c>BitMask</c> so
/// ported modules and future party-targeted mechanics can use it directly. Thin wrapper over a ulong.
/// </summary>
public struct BitMask(ulong raw = 0)
{
    public ulong Raw = raw;

    public readonly bool None => this.Raw == 0;
    public readonly bool Any => this.Raw != 0;
    public readonly int NumSetBits => System.Numerics.BitOperations.PopCount(this.Raw);

    public readonly bool this[int index] => (this.Raw & (1ul << index)) != 0;

    public void Set(int index) => this.Raw |= 1ul << index;
    public void Clear(int index) => this.Raw &= ~(1ul << index);
    public void Toggle(int index) => this.Raw ^= 1ul << index;

    public readonly int LowestSetBit() => this.Raw == 0 ? -1 : System.Numerics.BitOperations.TrailingZeroCount(this.Raw);

    public static BitMask Build(params int[] bits)
    {
        var m = default(BitMask);
        foreach (var b in bits)
            m.Set(b);
        return m;
    }

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
}
