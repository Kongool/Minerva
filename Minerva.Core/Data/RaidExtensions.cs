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

    /// <summary>Filter a <c>Raid.WithSlot()</c> result by slot index (BMR's <c>WhereSlot</c>).</summary>
    public static IEnumerable<(int slot, Actor actor)> WhereSlot(this (int slot, Actor actor)[] party, Func<int, bool> predicate)
    {
        for (var i = 0; i < party.Length; ++i)
            if (predicate(party[i].slot))
                yield return party[i];
    }

    /// <summary>Filter a <c>Raid.WithSlot()</c> result by actor (BMR's <c>WhereActor</c>).</summary>
    public static IEnumerable<(int slot, Actor actor)> WhereActor(this (int slot, Actor actor)[] party, Func<Actor, bool> predicate)
    {
        for (var i = 0; i < party.Length; ++i)
            if (predicate(party[i].actor))
                yield return party[i];
    }

    /// <summary>Project a slotted party sequence to just the actors (BMR's <c>Actors</c>).</summary>
    public static IEnumerable<Actor> Actors(this IEnumerable<(int slot, Actor actor)> party)
    {
        foreach (var p in party)
            yield return p.actor;
    }

    /// <summary>The actor nearest <paramref name="pos"/>, or null if the sequence is empty (BMR's <c>Closest</c>).</summary>
    public static Actor? Closest(this IEnumerable<Actor> actors, WPos pos)
    {
        Actor? best = null;
        var bestSq = float.MaxValue;
        foreach (var a in actors)
        {
            var d = (a.Position - pos).LengthSq();
            if (d < bestSq) { bestSq = d; best = a; }
        }
        return best;
    }
}
