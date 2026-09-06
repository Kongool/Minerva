namespace Minerva.Global.MaskedCarnivale;

/// <summary>
/// The handful of arena shapes every Masked Carnivale stage is fought in, ported from BossmodReborn
/// (BSD-3; see THIRD-PARTY-NOTICES.txt). The Carnivale reuses one room with a few furniture layouts, so
/// 50-odd stage modules share these rather than each declaring its own bounds.
///
/// <para>BMR's two <c>*Blockers</c> fields are omitted: they are <c>RelSimplifiedComplexPolygon</c> values
/// produced by its polygon clipper, which Minerva does not have. Three stage files use them and stay
/// staged; the other 113 references across the Carnivale are to the three simple members below.</para>
/// </summary>
public static class Layouts
{
    public static readonly WPos ArenaCenter = new(100f, 100f);

    private static readonly Angle a45 = 45f.Degrees();
    private static readonly Angle a90a = 89.98f.Degrees(), a90b = 90f.Degrees();
    private static readonly Polygon[] circleBig = [new Polygon(ArenaCenter, 25f, 64)];
    private static readonly Polygon[] circleBigAdj = [new Polygon(ArenaCenter, 24.5f, 64)];
    private static readonly Rectangle[] walls =
    [
        new(new(90f, 94.75f), 5f, 0.25f), new(new(94.75f, 92f), 3.01134109f, 0.25f, a90a),
        new(new(110f, 94.75f), 5f, 0.25f), new(new(105.25f, 92f), 3.01134109f, 0.25f, a90b),
    ];
    private static readonly Square[] bigQuad = [new Square(ArenaCenter, 4.9f, a45)];
    private const float SideLength = 1.9f;
    private static readonly Square[] squares =
    [
        new(new(110f, 110f), SideLength, a45), new(new(90f, 110f), SideLength, a45),
        new(new(110f, 90f), SideLength, a45), new(new(90f, 90f), SideLength, a45),
    ];

    public static readonly ArenaBoundsCustom Layout4Quads = new(circleBig, squares, AdjustForHitboxInwards: true);
    public static readonly ArenaBoundsCustom Layout2Corners = new(circleBig, walls, AdjustForHitboxInwards: true);
    public static readonly ArenaBoundsCustom LayoutBigQuad = new(circleBig, bigQuad, AdjustForHitboxInwards: true);

    // The same two arenas as line-of-sight blockers rather than as bounds: a stage where the pillars break
    // sight needs the polygon, not the walkable region, and it is built from the ADJUSTED circle so the
    // blocker geometry lines up with the bounds the player is actually confined to.
    public static readonly RelSimplifiedComplexPolygon Layout2CornersBlockers = PolygonClipper.GetCombinedPolygon(ArenaCenter, circleBigAdj, walls);
    public static readonly RelSimplifiedComplexPolygon LayoutBigQuadBlockers = PolygonClipper.GetCombinedPolygon(ArenaCenter, circleBigAdj, bigQuad);
    public static readonly ArenaBoundsCustom CircleSmall = new([new Polygon(ArenaCenter, 16.01379f, 32)]);
    public static readonly ArenaBoundsCustom CircleBig = new(circleBigAdj);
}
