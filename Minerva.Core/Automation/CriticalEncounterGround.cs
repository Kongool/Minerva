using System.Reflection;

namespace Minerva;

/// <summary>
/// Where a critical encounter lets a character be when no module is running its fight for them. Two sides of one wall:
/// <list type="bullet">
/// <item>While it is open to join, inside its circle. Whoever is in the circle when the screen fades is in the fight;
/// whoever is not is put outside. Quarried Away, 2026-10-03: someone dragged two Crescent Rot-eyes through as it was
/// about to start, and Korha and Saar went after them or away from them, to 27.2y and 27.5y from the centre of a 27y
/// circle. Rosa and Xia, at 20y, fought it; Korha and Saar watched from outside.</item>
/// <item>Once it has started without them (<see cref="ModuleBase.ShutOut"/>), outside its wall. Saar, the same fight: the
/// dodge picked spots inside, the walk followed a Light Aether in, and she slid along the wall for both.</item>
/// </list>
/// Either way it is ground the dodge and the walk to a target may not use, and a target on the far side is out of reach.
/// </summary>
public static class CriticalEncounterGround
{
    /// <summary>How far inside the join circle a waiting character is kept. A dodge stops a little past where it aims,
    /// and the circle is the whole difference between fighting and watching.</summary>
    public const float JoinMargin = 3f;

    /// <summary>How far past the floor the wall reaches, seen from outside, for an encounter whose join circle is not
    /// known. Where it is, the wall is the circle: Saar stood against Quarried Away's at 27.6y from the centre, its 27y
    /// circle plus her half-yalm, three past the 24.5y floor.</summary>
    public const float WallThickness = 2f;

    // A character's own radius: how far from a wall its centre stops.
    private const float HalfYalm = 0.5f;

    /// <summary>
    /// The join circles of North Horn's critical encounters, by DynamicEvent row, as BOCCHI reads them from each one's
    /// map range in the zone layout (dalamud.log, 2026-10-03). The floor a module draws does not predict them: Tiny
    /// Terror fights on 20y and joins at 27, Lost on the Wind 24 and 32.
    /// </summary>
    private static readonly Dictionary<uint, (WPos Center, float Radius)> KnownJoinCircles = new()
    {
        [50] = (new WPos(-215f, -65f), 24.5f), // Double Trouble
        [51] = (new WPos(-519f, -641f), 27f),  // Quarried Away
        [52] = (new WPos(659f, 659f), 27f),    // Forbidden Folios
        [56] = (new WPos(238f, 352f), 27f),    // A Beast Unleashed
        [57] = (new WPos(224f, -860f), 27f),   // Dark Artistry
        [58] = (new WPos(-390f, 700f), 32f),   // Familiar Tactics
        [59] = (new WPos(807f, -562f), 27f),   // Appalling Behavior
        [60] = (new WPos(152f, 716f), 27f),    // Tiny Terror
        [61] = (new WPos(-150f, -860f), 32f),  // Lost on the Wind
        [62] = (new WPos(-82f, 485f), 22f),    // Ahead of the Competition
        [63] = (new WPos(500f, -310f), 32f),   // Accept No Imitators
        [64] = (new WPos(-320f, 422f), 20f),
    };

    /// <summary>The smallest join circle known. An encounter not in the table is given this, so a character held in it
    /// is inside whatever the real one is.</summary>
    public const float SmallestJoinRadius = 20f;

    /// <summary>
    /// An encounter's join circle: the known one, else its map marker's centre with <see cref="SmallestJoinRadius"/>.
    /// The marker's own radius is no help: Dark Artistry's, 2026-10-03, read 0 against BOCCHI's 27y, though its centre
    /// matched to the yalm.
    /// </summary>
    public static (WPos Center, float Radius)? JoinCircle(uint dynamicEventId, WPos markerCenter)
    {
        if (KnownJoinCircles.TryGetValue(dynamicEventId, out var known))
            return known;
        return markerCenter != default ? (markerCenter, SmallestJoinRadius) : null;
    }

    /// <summary>Is this encounter's join circle one of the known ones, rather than the smallest round its marker?</summary>
    public static bool IsKnown(uint dynamicEventId) => KnownJoinCircles.ContainsKey(dynamicEventId);

    /// <summary>
    /// Everything beyond <see cref="JoinMargin"/> inside the join circle, for a player standing within that; null for
    /// one who is not, so walking out on purpose is never pulled back.
    /// </summary>
    public static ShapeDistance? KeepIn(WPos center, float joinRadius, WPos player)
    {
        var inner = joinRadius - JoinMargin;
        return inner > 0f && (player - center).LengthSq() <= inner * inner ? new SDInvertedCircle(center, inner) : null;
    }

    /// <summary>A walled fight's floor and wall, for a player shut outside it: its join circle when known (the module's
    /// name id is the DynamicEvent row), else its floor grown by <see cref="WallThickness"/>.</summary>
    public static ShapeDistance KeepOut(ModuleBase module)
        => module.GetType().GetCustomAttribute<ModuleInfoAttribute>() is { } info && KnownJoinCircles.TryGetValue(info.NameID, out var join)
            ? new SDCircle(join.Center, join.Radius + HalfYalm)
            : new SDArenaFloor(module.Bounds, module.Center, WallThickness);
}
