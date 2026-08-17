using System.Runtime.InteropServices;

namespace Minerva;

/// <summary>
/// Small helpers that mirror BossmodReborn utilities so ported modules compile unchanged (BSD-3; see
/// THIRD-PARTY-NOTICES.txt): a by-ref list accessor and a slot-set builder over party arrays.
/// </summary>
public static class RaidExtensions
{
    /// <summary>Access a list element by reference (BMR's <c>List.Ref</c>).</summary>
    public static ref T Ref<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];

    /// <summary>Build a <see cref="BitMask"/> of the slot indices in a <c>Raid.WithSlot()</c> result.</summary>
    public static BitMask Mask(this (int slot, Actor actor)[] party)
    {
        var m = default(BitMask);
        for (var i = 0; i < party.Length; ++i)
            m.Set(party[i].slot);
        return m;
    }
}
