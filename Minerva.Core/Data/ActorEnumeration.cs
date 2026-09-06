namespace Minerva;

/// <summary>
/// Query helpers over actor ranges, ported from BossmodReborn's <c>ActorEnumeration</c> (BSD-3; see
/// THIRD-PARTY-NOTICES.txt). Every helper comes in two forms — over bare <see cref="Actor"/> and over
/// <c>(slot, Actor)</c> pairs — because a module that needs to set a bit per party slot has to keep the slot
/// index alongside the actor all the way through the query.
///
/// <para>These are the vocabulary ported modules are written in: <c>Raid.WithoutSlot().InRadius(...)</c>
/// reads the way the fight is described. Missing them accounted for the largest share of body-level errors
/// in the import.</para>
///
/// <para><see cref="RaidExtensions"/> already declares array-typed <c>Mask</c>/<c>WhereSlot</c>/
/// <c>WhereActor</c> and <c>Closest(IEnumerable&lt;Actor&gt;, WPos)</c> plus <c>Actors</c>. The array forms
/// are more specific overloads and win where they apply; the two exact-signature duplicates are deliberately
/// not repeated here, since two extension methods with identical signatures make every call site ambiguous.
/// </para>
/// </summary>
public static class ActorEnumeration
{
    /// <summary>A mask with a bit set for each slot in the range.</summary>
    public static BitMask Mask(this IEnumerable<(int, Actor)> range)
    {
        BitMask mask = default;
        foreach (var (i, _) in range)
            mask[i] = true;
        return mask;
    }

    public static IEnumerable<(int, Actor)> WhereSlot(this IEnumerable<(int, Actor)> range, Func<int, bool> predicate)
    {
        foreach (var item in range)
            if (predicate(item.Item1))
                yield return item;
    }

    public static IEnumerable<(int, Actor)> WhereActor(this IEnumerable<(int, Actor)> range, Func<Actor, bool> predicate)
    {
        foreach (var item in range)
            if (predicate(item.Item2))
                yield return item;
    }

    public static IEnumerable<Actor> Exclude(this IEnumerable<Actor> range, Actor? actor)
    {
        foreach (var x in range)
            if (x != actor)
                yield return x;
    }

    public static IEnumerable<Actor> Exclude(this IEnumerable<Actor> range, IEnumerable<Actor> actors)
    {
        var excluded = new HashSet<Actor>(actors);
        foreach (var x in range)
            if (!excluded.Contains(x))
                yield return x;
    }

    public static IEnumerable<(int, Actor)> Exclude(this IEnumerable<(int, Actor)> range, Actor? actor) => range.WhereActor(x => x != actor);

    public static IEnumerable<(int, Actor)> Exclude(this IEnumerable<(int, Actor)> range, int slot) => range.WhereSlot(i => i != slot);

    public static IEnumerable<(int, Actor)> IncludedInMask(this IEnumerable<(int, Actor)> range, BitMask mask) => range.WhereSlot(i => mask[i]);

    public static IEnumerable<(int, Actor)> ExcludedFromMask(this IEnumerable<(int, Actor)> range, BitMask mask) => range.WhereSlot(i => !mask[i]);

    public static IEnumerable<Actor> InRadius(this IEnumerable<Actor> range, WPos origin, float radius)
    {
        foreach (var actor in range)
            if (actor.Position.InCircle(origin, radius))
                yield return actor;
    }

    public static IEnumerable<(int, Actor)> InRadius(this IEnumerable<(int, Actor)> range, WPos origin, float radius)
        => range.WhereActor(actor => actor.Position.InCircle(origin, radius));

    public static IEnumerable<Actor> OutOfRadius(this IEnumerable<Actor> range, WPos origin, float radius)
    {
        foreach (var actor in range)
            if (!actor.Position.InCircle(origin, radius))
                yield return actor;
    }

    public static IEnumerable<(int, Actor)> OutOfRadius(this IEnumerable<(int, Actor)> range, WPos origin, float radius)
        => range.WhereActor(actor => !actor.Position.InCircle(origin, radius));

    /// <summary>Actors within <paramref name="radius"/> of <paramref name="origin"/>, not counting it.</summary>
    public static IEnumerable<Actor> InRadiusExcluding(this IEnumerable<Actor> range, Actor origin, float radius)
        => range.Exclude(origin).InRadius(origin.Position, radius);

    public static IEnumerable<(int, Actor)> InRadiusExcluding(this IEnumerable<(int, Actor)> range, Actor origin, float radius)
        => range.Exclude(origin).InRadius(origin.Position, radius);

    public static List<Actor> InShape(this IEnumerable<Actor> range, AOEShape shape, WPos origin, Angle rotation)
    {
        List<Actor> result = [];
        foreach (var actor in range)
            if (shape.Check(actor.Position, origin, rotation))
                result.Add(actor);
        return result;
    }

    public static List<Actor> InShape(this IEnumerable<Actor> range, AOEShape shape, Actor origin)
        => range.InShape(shape, origin.Position, origin.Rotation);

    public static List<(int, Actor)> InShape(this IEnumerable<(int, Actor)> range, AOEShape shape, WPos origin, Angle rotation)
    {
        List<(int, Actor)> result = [];
        foreach (var tuple in range)
            if (shape.Check(tuple.Item2.Position, origin, rotation))
                result.Add(tuple);
        return result;
    }

    /// <summary>Actors tethered with the given tether id.</summary>
    public static IEnumerable<Actor> Tethered<ID>(this IEnumerable<Actor> range, ID id) where ID : Enum
    {
        var tetherID = (uint)(object)id;
        foreach (var actor in range)
            if (actor.Tether.ID == tetherID)
                yield return actor;
    }

    public static IEnumerable<(int, Actor)> Tethered<ID>(this IEnumerable<(int, Actor)> range, ID id) where ID : Enum
    {
        var tetherID = (uint)(object)id;
        return range.WhereActor(actor => actor.Tether.ID == tetherID);
    }

    public static IEnumerable<Actor> SortedByRange(this IEnumerable<Actor> range, WPos origin)
    {
        var actors = new List<(Actor actor, float distSq)>();
        foreach (var a in range)
            actors.Add((a, (a.Position - origin).LengthSq()));
        actors.Sort(static (a, b) => a.distSq.CompareTo(b.distSq));
        for (var i = 0; i < actors.Count; ++i)
            yield return actors[i].actor;
    }

    public static IEnumerable<(int, Actor)> SortedByRange(this IEnumerable<(int, Actor)> range, WPos origin)
    {
        var actors = new List<(int slot, Actor actor, float distSq)>();
        foreach (var a in range)
            actors.Add((a.Item1, a.Item2, (a.Item2.Position - origin).LengthSq()));
        actors.Sort(static (a, b) => a.distSq.CompareTo(b.distSq));
        for (var i = 0; i < actors.Count; ++i)
            yield return (actors[i].slot, actors[i].actor);
    }

    public static (int, Actor) Closest(this IEnumerable<(int, Actor)> range, WPos origin)
    {
        (int, Actor)? closest = null;
        var minDistSq = float.MaxValue;
        foreach (var (slot, actor) in range)
        {
            var distSq = (actor.Position - origin).LengthSq();
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = (slot, actor);
            }
        }
        return closest!.Value;
    }

    public static Actor? Farthest(this IEnumerable<Actor> range, WPos origin)
    {
        Actor? farthest = null;
        var maxDistSq = float.MinValue;
        foreach (var actor in range)
        {
            var distSq = (actor.Position - origin).LengthSq();
            if (distSq > maxDistSq)
            {
                maxDistSq = distSq;
                farthest = actor;
            }
        }
        return farthest;
    }

    /// <summary>How many actors match the condition, and how many do not.</summary>
    public static (int match, int mismatch) CountByCondition(this IEnumerable<Actor> range, Func<Actor, bool> condition)
    {
        int match = 0, mismatch = 0;
        foreach (var a in range)
        {
            if (condition(a))
                ++match;
            else
                ++mismatch;
        }
        return (match, mismatch);
    }

    /// <summary>
    /// The range ordered around <paramref name="center"/> starting from <paramref name="starting"/>, going
    /// clockwise unless <paramref name="counterclockwise"/>. Mechanics that resolve "in order from the
    /// marked player" need this ordering to assign spots.
    /// </summary>
    public static IEnumerable<Actor> ClockOrder(this IEnumerable<Actor> range, Actor starting, WPos center, bool counterclockwise = false)
    {
        var startingAngle = (starting.Position - center).ToAngle();
        var list = new List<(Actor actor, float angle)>();
        foreach (var r in range)
            list.Add((r, UnwrapAngle((r.Position - center).ToAngle().Rad, startingAngle.Rad, r != starting, counterclockwise)));
        SortByAngle(list, counterclockwise);
        foreach (var (actor, _) in list)
            yield return actor;
    }

    public static IEnumerable<(int, Actor)> ClockOrder(this IEnumerable<(int, Actor)> range, Actor starting, WPos center, bool counterclockwise = false)
    {
        var startingAngle = (starting.Position - center).ToAngle();
        var list = new List<((int, Actor) item, float angle)>();
        foreach (var r in range)
            list.Add((r, UnwrapAngle((r.Item2.Position - center).ToAngle().Rad, startingAngle.Rad, r.Item2 != starting, counterclockwise)));
        SortByAngle(list, counterclockwise);
        foreach (var (item, _) in list)
            yield return item;
    }

    // shift everything past the starting actor by a full turn, so a plain sort walks the circle once from it
    private static float UnwrapAngle(float angle, float startingAngle, bool notStarting, bool counterclockwise)
    {
        if (!notStarting)
            return angle;
        if (counterclockwise)
            return angle < startingAngle ? angle + Angle.DoublePI : angle;
        return angle > startingAngle ? angle - Angle.DoublePI : angle;
    }

    private static void SortByAngle<T>(List<(T item, float angle)> list, bool counterclockwise)
    {
        if (counterclockwise)
            list.Sort(static (a, b) => a.angle.CompareTo(b.angle));
        else
            list.Sort(static (a, b) => b.angle.CompareTo(a.angle));
    }
}
