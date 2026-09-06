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

/// <summary>Sorting helpers matching BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public static class SortHelpers
{
    public static void SortAOEByActivation(List<AOEInstance> aoes)
        => aoes.Sort(static (a, b) => a.Activation.CompareTo(b.Activation));

    /// <summary>Stable order by the actor that owns each AOE — for fights where several casters put up the
    /// same shape and a component needs to pair an AOE back to its caster by position in the list.</summary>
    public static void SortAOEsByActorID(List<AOEInstance> aoes)
        => aoes.Sort(static (a, b) => a.ActorID.CompareTo(b.ActorID));

    /// <summary>Actors newest-first. Spawn order is the tiebreak a fight uses when several identical adds
    /// resolve in sequence.</summary>
    public static void SortActorsByIDDescending(List<Actor> actors)
        => actors.Sort(static (a, b) => b.InstanceID.CompareTo(a.InstanceID));

    /// <inheritdoc cref="SortActorsByIDDescending"/>
    public static void SortActorsByID(List<Actor> actors)
        => actors.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID));
}
