namespace Minerva;

/// <summary>
/// The playable extent of an arena, centered at the module's arena center. Drives the radar's
/// world→screen scale (via <see cref="Extent"/>), draws the boundary, and answers whether a point
/// is inside the field.
/// </summary>
public abstract class ArenaBounds(float radius, float extent = -1f)
{
    /// <summary>
    /// BossmodReborn's radius, which ported modules compare: a circle's radius, the longer half-side of a square or
    /// rectangle, the furthest a polygon reaches along either axis.
    /// <para>It used to be the corner distance (a square's half-diagonal), and a port that asks "is the arena still
    /// the big one" got the wrong answer on every square. Moko the Restless, 2026-10-18: its arena-change band is
    /// drawn while <c>Bounds.Radius &gt; 21</c>; after the floor shrank to a 20y square that read 28.3, so every later
    /// Kenki Release drew the band again, and the floor change that clears it never came twice.</para>
    /// </summary>
    public readonly float Radius = radius;

    /// <summary>How far the floor reaches from its centre at most, corners included: what the radar fits on screen
    /// and the solver searches. A circle's radius, a square's half-diagonal.</summary>
    public readonly float Extent = extent >= 0f ? extent : radius;

    /// <summary>
    /// Cell size of the pathfinding grid over this arena, in yalms. Modules read it to size a target cell
    /// (see <see cref="SDPrecisePosition"/>) so "stand here" means one cell rather than an exact point the
    /// search can never land on.
    /// </summary>
    public float MapResolution { get; init; } = 0.5f;

    /// <summary>
    /// How far a tessellated curve may sit from the true one, in yalms — the error budget a module uses when
    /// it builds its own polygon and needs it to agree with the arena's outline.
    ///
    /// <para>Scales with the arena, since a fixed tolerance that looks fine at 20y is visibly wrong at 50y,
    /// and with <see cref="MapResolution"/>, because approximating finer than the grid the pathfinder
    /// samples on buys nothing.</para>
    /// </summary>
    public float MaxApproxError => this.MapResolution * 0.05f * this.Radius;

    /// <summary>
    /// The boundary as a polygon, relative to the arena centre. Knockback resolution needs it: "would this
    /// knockback throw me off the edge" is a containment test against the real outline, which for anything
    /// but a circle cannot be answered by a radius.
    /// <para>Built once on first use from <see cref="Contour"/>, so it costs the same tessellation the
    /// shape already hit-tests against and cannot disagree with it.</para>
    /// </summary>
    public RelSimplifiedComplexPolygon Shape
        => field ??= new([new RelPolygonWithHoles([.. ContourOffsets(this.Contour(default))])]);

    private static IEnumerable<WDir> ContourOffsets(IReadOnlyList<WPos> contour)
    {
        foreach (var p in contour)
            yield return new WDir(p.X, p.Z);
    }

    /// <summary>The nearest point inside the arena, walking in from <paramref name="offset"/> relative to the
    /// centre. Returns the offset unchanged when it is already inside.</summary>
    public WDir ClampToBounds(WDir offset)
    {
        var len = offset.Length();
        if (len < 1e-4f)
            return offset;
        var dir = offset / len;
        var reach = this.IntersectRay(default, default, dir);
        return len <= reach ? offset : dir * reach;
    }

    private bool[]? outsideMask;
    private WPos maskCenter, maskOrigin;
    private float maskCell;
    private int maskW, maskH;

    /// <summary>
    /// Fill <paramref name="into"/> with "this cell is off the floor", for a grid of
    /// <paramref name="w"/> x <paramref name="h"/> cells of <paramref name="cell"/> yalms starting at
    /// <paramref name="origin"/>.
    ///
    /// <para>Worked out once and kept. The shape cannot change -- a module that opens or closes part of its
    /// floor swaps the whole bounds object, which brings a fresh cache with it -- while the pathfinder asks
    /// for this on every grid it builds, four of them per solve, on every frame. Against a many-sided
    /// polygon that was a point-in-polygon test per cell per grid, which is the single biggest fixed cost
    /// in a critical engagement.</para>
    /// </summary>
    public void CopyOutsideMask(WPos center, WPos origin, float cell, int w, int h, bool[] into)
    {
        if (this.outsideMask == null || this.maskW != w || this.maskH != h || this.maskCell != cell
            || !this.maskCenter.AlmostEqual(center, 0.01f) || !this.maskOrigin.AlmostEqual(origin, 0.01f))
        {
            var mask = new bool[w * h];
            for (var z = 0; z < h; ++z)
            {
                for (var x = 0; x < w; ++x)
                {
                    var p = new WPos(origin.X + (x * cell), origin.Z + (z * cell));
                    mask[(z * w) + x] = !this.Contains(center, p);
                }
            }

            this.outsideMask = mask;
            this.brokenSteps = this.FindBrokenSteps(center, origin, cell, w, h, mask);
            this.maskCenter = center;
            this.maskOrigin = origin;
            this.maskCell = cell;
            this.maskW = w;
            this.maskH = h;
        }

        Array.Copy(this.outsideMask, into, Math.Min(this.outsideMask.Length, into.Length));
    }

    private byte[]? brokenSteps;

    /// <summary>
    /// For the grid last given to <see cref="CopyOutsideMask"/>: per cell, a bit (<see cref="StepBit"/>) for each
    /// neighbour that is on the floor while the straight step to it is not. Null when the floor has no such step, which
    /// is every convex floor.
    /// <para>The grid knows the floor at cell centres, and two neighbouring centres can both be floor with the floor's
    /// edge between them: the notch where a round platform meets a ring. A search that steps cell to cell walked
    /// through it. Forgiven Treachery, 2026-11-21: Saar's way from the west platform onto the ring ran through that
    /// notch, he stood pressed into the corner for 6.6s and took the cone he was leaving, and Korha did the same at the
    /// east platform.</para>
    /// </summary>
    public byte[]? BrokenSteps => this.brokenSteps;

    /// <summary>The bit in <see cref="BrokenSteps"/> for the neighbour at (<paramref name="dx"/>, <paramref name="dz"/>).</summary>
    public static int StepBit(int dx, int dz)
    {
        var k = ((dz + 1) * 3) + dx + 1;
        return 1 << (k > 4 ? k - 1 : k);
    }

    private byte[]? FindBrokenSteps(WPos center, WPos origin, float cell, int w, int h, bool[] outside)
    {
        byte[]? broken = null;
        for (var z = 0; z < h; ++z)
        {
            for (var x = 0; x < w; ++x)
            {
                var i = (z * w) + x;
                if (outside[i])
                    continue;

                // each pair once: east, south, and the two southward diagonals
                for (var d = 0; d < 4; ++d)
                {
                    var dx = d == 3 ? -1 : d == 1 ? 0 : 1;
                    var dz = d == 0 ? 0 : 1;
                    var nx = x + dx;
                    var nz = z + dz;
                    if (nx < 0 || nx >= w || nz >= h)
                        continue;
                    var j = (nz * w) + nx;
                    // only where the floor's edge is near: a step with floor all round both ends cannot cross it
                    if (outside[j] || (!NearOutside(outside, w, h, x, z) && !NearOutside(outside, w, h, nx, nz)))
                        continue;

                    var a = new WPos(origin.X + (x * cell), origin.Z + (z * cell));
                    var step = new WDir(dx * cell, dz * cell);
                    var parts = dx != 0 && dz != 0 ? 6 : 4;   // about a quarter of a yalm apart on a one-yalm grid
                    for (var s = 1; s < parts; ++s)
                    {
                        if (this.Contains(center, a + (step * ((float)s / parts))))
                            continue;
                        broken ??= new byte[w * h];
                        broken[i] |= (byte)StepBit(dx, dz);
                        broken[j] |= (byte)StepBit(-dx, -dz);
                        break;
                    }
                }
            }
        }

        return broken;
    }

    private static bool NearOutside(bool[] outside, int w, int h, int x, int z)
    {
        for (var dz = -1; dz <= 1; ++dz)
        {
            for (var dx = -1; dx <= 1; ++dx)
            {
                var nx = x + dx;
                var nz = z + dz;
                if (nx >= 0 && nx < w && nz >= 0 && nz < h && outside[(nz * w) + nx])
                    return true;
            }
        }

        return false;
    }

    // The floor on the lattice ArenaPathfinder.Doorway walks; see LatticeInside.
    private bool[]? latticeInside;
    private WPos latticeCenter;
    private float latticeCell;
    private int latticeSteps;

    /// <summary>
    /// Which points of the lattice from -<see cref="Extent"/> to +<see cref="Extent"/> round <paramref name="center"/>,
    /// <paramref name="cell"/> apart, are on the floor: index (i * steps) + j for the i-th x and j-th z. The points are
    /// made by the same repeated addition as the loops that read them, so each answer is <see cref="Contains"/> of the
    /// very point asked about.
    /// <para>Kept on the bounds like <see cref="CopyOutsideMask"/>. The last-resort search asked it of every point,
    /// for each of two margins and again for each door: on Forbidden Folios' polygon floor, 2026-10-19, that was half
    /// of a 25ms solve.</para>
    /// </summary>
    internal bool[] LatticeInside(WPos center, float cell, out int steps)
    {
        if (this.latticeInside == null || this.latticeCenter != center || this.latticeCell != cell)
        {
            var r = this.Extent;
            var n = 0;
            for (var x = -r; x <= r; x += cell)
                ++n;
            var inside = new bool[n * n];
            var i = 0;
            for (var x = -r; x <= r; x += cell, ++i)
            {
                var j = 0;
                for (var z = -r; z <= r; z += cell, ++j)
                    inside[(i * n) + j] = this.Contains(center, center + new WDir(x, z));
            }

            this.latticeInside = inside;
            this.latticeCenter = center;
            this.latticeCell = cell;
            this.latticeSteps = n;
        }

        steps = this.latticeSteps;
        return this.latticeInside;
    }

    public abstract bool Contains(WPos center, WPos point);

    /// <summary>
    /// The same floor with its outer edge pulled in by <paramref name="margin"/> yalms: where the dodge may stand
    /// when that edge is a wall that kills (see <c>ModuleBase.BarrierMargin</c>). Obstacles inside the floor are
    /// not moved. Shapes that do not say how to shrink come back unchanged.
    /// </summary>
    public virtual ArenaBounds Inset(float margin) => this;

    /// <summary>
    /// Is a point inside, given as an OFFSET from the arena centre? BossmodReborn's form, which its
    /// modules call as <c>Bounds.Contains(pc.Position - Center)</c>.
    ///
    /// <para>Minerva's own form takes the centre explicitly because its shapes are described in world
    /// space, which is why this cannot simply be the same method: for the ordinary bounds the centre is
    /// only a reference point and passing the origin gives the right answer, but a custom arena's shapes
    /// carry their own absolute coordinates and it overrides this accordingly.</para>
    /// </summary>
    public virtual bool Contains(in WDir offset) => this.Contains(default, new WPos(offset.X, offset.Z));

    /// <summary>
    /// Distance from <paramref name="origin"/> along <paramref name="dir"/> to the boundary. Solved
    /// generically off <see cref="Contains"/> — march out to find the first outside sample, then bisect —
    /// so it works for every bounds shape including custom polygons, at the cost of being approximate
    /// (~1cm). Returns 0 if the origin is already outside.
    /// </summary>
    /// <summary>
    /// Distance from an arena-relative offset to the boundary along <paramref name="dir"/>.
    ///
    /// <para>BossmodReborn's shape: its bounds are always expressed relative to the arena centre, so a
    /// module asking "how far to the wall from here" passes an offset rather than a world position. The
    /// three-argument form below is the same query in absolute coordinates.</para>
    /// </summary>
    public float IntersectRay(WDir originOffset, WDir dir)
    {
        var centre = new WPos(0f, 0f);
        return this.IntersectRay(centre, centre + originOffset, dir);
    }

    public float IntersectRay(WPos center, WPos origin, WDir dir)
    {
        if (!this.Contains(center, origin))
            return 0f;
        var far = this.Extent * 2f + 1f;
        var lo = 0f;
        var hi = far;
        // if even the far sample is inside (shouldn't happen for sane bounds), report the far distance
        if (this.Contains(center, origin + far * dir))
            return far;
        for (var i = 0; i < 24; ++i) // 24 halvings of <=81y is well under a centimetre
        {
            var mid = 0.5f * (lo + hi);
            if (this.Contains(center, origin + mid * dir))
                lo = mid;
            else
                hi = mid;
        }
        return lo;
    }

    /// <summary>Closed outer boundary polygon in world space, given the arena center.</summary>
    public abstract IReadOnlyList<WPos> Contour(WPos center);

    /// <summary>Inner boundary loop for ring arenas (a donut hole), or null when the field is solid.</summary>
    public virtual IReadOnlyList<WPos>? InnerContour(WPos center) => null;

    /// <summary>
    /// Interior cut-outs the field excludes — boulders, pillars, rubble. A ring arena's single hole is
    /// <see cref="InnerContour"/>; this carries an arbitrary number of them, so the radar can draw every
    /// obstacle rather than just one. Fights where the mechanic *is* the obstacle (hide behind a rock to
    /// break line of sight) are unplayable if the player can only see the first.
    /// </summary>
    public virtual IReadOnlyList<IReadOnlyList<WPos>> Obstacles(WPos center) => [];

    protected static List<WPos> Circle(WPos center, float radius, int segments = 60)
    {
        var pts = new List<WPos>(segments + 1);
        var step = Angle.TwoPI / segments;
        for (var i = 0; i <= segments; ++i)
        {
            var a = new Angle(step * i);
            pts.Add(center + a.ToDirection() * radius);
        }
        return pts;
    }
}

public sealed class ArenaBoundsCircle(float radius) : ArenaBounds(radius)
{
    public override bool Contains(WPos center, WPos point) => point.InCircle(center, this.Radius);
    public override IReadOnlyList<WPos> Contour(WPos center) => Circle(center, this.Radius);
    public override ArenaBounds Inset(float margin) => new ArenaBoundsCircle(MathF.Max(this.Radius - margin, 0f)) { MapResolution = this.MapResolution };
}

/// <remarks>
/// <paramref name="rotation"/> turns the square; the remaining parameters exist for source compatibility
/// with BossmodReborn and are not acted on — Minerva has no obstacle-map layer, and it rasterises the
/// pathfinding grid at a cell size the solver chooses rather than one baked into the bounds.
/// </remarks>
public sealed class ArenaBoundsSquare(float halfWidth, Angle rotation = default, float mapResolution = 0.5f, bool allowObstacleMap = false) : ArenaBounds(halfWidth, halfWidth * 1.41421356f)
{
    public readonly float HalfWidth = halfWidth;

    public override bool Contains(WPos center, WPos point)
        => MathF.Abs(point.X - center.X) <= this.HalfWidth && MathF.Abs(point.Z - center.Z) <= this.HalfWidth;

    public override ArenaBounds Inset(float margin) => new ArenaBoundsSquare(MathF.Max(this.HalfWidth - margin, 0f)) { MapResolution = this.MapResolution };

    public override IReadOnlyList<WPos> Contour(WPos center)
    {
        var h = this.HalfWidth;
        return
        [
            new(center.X - h, center.Z - h),
            new(center.X + h, center.Z - h),
            new(center.X + h, center.Z + h),
            new(center.X - h, center.Z + h),
        ];
    }
}

public sealed class ArenaBoundsRect(float halfWidth, float halfHeight)
    : ArenaBounds(MathF.Max(halfWidth, halfHeight), MathF.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight))
{
    public readonly float HalfWidth = halfWidth;
    public readonly float HalfHeight = halfHeight;

    public override bool Contains(WPos center, WPos point)
        => MathF.Abs(point.X - center.X) <= this.HalfWidth && MathF.Abs(point.Z - center.Z) <= this.HalfHeight;

    public override IReadOnlyList<WPos> Contour(WPos center)
    {
        var w = this.HalfWidth;
        var h = this.HalfHeight;
        return
        [
            new(center.X - w, center.Z - h),
            new(center.X + w, center.Z - h),
            new(center.X + w, center.Z + h),
            new(center.X - w, center.Z + h),
        ];
    }
}

/// <summary>
/// A ring arena: inside the outer radius but outside the inner radius. Common for fights that put a
/// hazard (or a pit) in the middle so players fight along the rim.
/// </summary>
public sealed class ArenaBoundsDonut(float innerRadius, float outerRadius) : ArenaBounds(outerRadius)
{
    public readonly float InnerRadius = innerRadius;
    public readonly float OuterRadius = outerRadius;

    public override bool Contains(WPos center, WPos point) => point.InDonut(center, this.InnerRadius, this.OuterRadius);
    public override IReadOnlyList<WPos> Contour(WPos center) => Circle(center, this.OuterRadius);
    public override IReadOnlyList<WPos> InnerContour(WPos center) => Circle(center, this.InnerRadius);
}

/// <summary>
/// An arbitrary-polygon arena, defined by vertices given as offsets from the arena center (the same
/// "relative bounds" model BMR uses, so a boss's real arena outline can be pasted in). Handles convex
/// and concave shapes; <see cref="Contains"/> is an even-odd ray cast.
/// </summary>
public sealed class ArenaBoundsPolygon(params WDir[] offsets) : ArenaBounds(MaxAxisOf(offsets), MaxRadiusOf(offsets))
{
    public readonly WDir[] Offsets = offsets;

    public override bool Contains(WPos center, WPos point)
    {
        var px = point.X - center.X;
        var pz = point.Z - center.Z;
        var inside = false;
        var v = this.Offsets;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
        {
            // edge (v[j] -> v[i]) crosses the horizontal ray to +X from the test point?
            if ((v[i].Z > pz) != (v[j].Z > pz)
                && px < (v[j].X - v[i].X) * (pz - v[i].Z) / (v[j].Z - v[i].Z) + v[i].X)
                inside = !inside;
        }
        return inside;
    }

    public override IReadOnlyList<WPos> Contour(WPos center)
    {
        var pts = new WPos[this.Offsets.Length];
        for (var i = 0; i < this.Offsets.Length; ++i)
            pts[i] = center + this.Offsets[i];
        return pts;
    }

    private static float MaxRadiusOf(WDir[] offsets)
    {
        var max = 0f;
        foreach (var o in offsets)
            max = MathF.Max(max, o.Length());
        return max;
    }

    private static float MaxAxisOf(WDir[] offsets)
    {
        var max = 0f;
        foreach (var o in offsets)
            max = MathF.Max(max, MathF.Max(MathF.Abs(o.X), MathF.Abs(o.Z)));
        return max;
    }
}

/// <summary>
/// A mesh arena composed of absolute-world <see cref="Shape"/>s: the field is inside any of the
/// <c>UnionShapes</c> and outside every <c>DifferenceShape</c> (holes/pits). Matches BossmodReborn's
/// <c>ArenaBoundsCustom</c> constructor (BSD-3; see THIRD-PARTY-NOTICES.txt) so DT arena definitions paste
/// in unchanged. Containment is exact (analytic per shape). <see cref="Contour"/> is only the largest union
/// operand's outline; the radar draws and masks the combined floor (<see cref="Polygon"/>, via ZoneMeshes).
/// Because the operands carry absolute coordinates, the arena center passed by the module is ignored here.
/// </summary>
public sealed class ArenaBoundsCustom : ArenaBounds
{
    public readonly Shape[] UnionShapes;
    public readonly Shape[] DifferenceShapes;

    /// <summary>Pieces put back after the differences are cut: an island in the middle of a hole. BossmodReborn's
    /// third array. It was taken and dropped until 2026-11-21, so the seven floors that pass one had no islands.</summary>
    public readonly Shape[] AdditionalShapes;

    /// <summary>The player's hitbox: how far BossmodReborn's AdjustForHitbox flags move a floor's edge.</summary>
    public const float HitboxRadius = 0.5f;

    /// <summary>Bounding-box centre of the union operands — what modules pass as the arena centre.</summary>
    public readonly WPos Center;

    private RelSimplifiedComplexPolygon? polygon;

    /// <summary>
    /// The union minus the differences, clipped into one polygon in arena-relative space.
    ///
    /// <para>Built on first use. Minerva answers <c>Contains</c> by testing the source shapes directly, so
    /// most arenas never need the clip; the ones that ask are the modules that manipulate the floor
    /// themselves — shrinking it for a knockback, carving out a collapsed section — and BossmodReborn's
    /// code for those is written against this representation.</para>
    /// </summary>
    public RelSimplifiedComplexPolygon Polygon
        => this.polygon ??= PolygonClipper.GetCombinedPolygon(this.Center, this.UnionShapes, this.DifferenceShapes, this.AdditionalShapes.Length > 0 ? this.AdditionalShapes : null);

    /// <summary>
    /// The union is a many-sided polygon standing in for a circle. Set by modules that build a round arena
    /// out of a <c>Polygon</c>, so a renderer can draw one smooth curve instead of 64 line segments.
    /// </summary>
    public bool IsCircle { get; init; }

    public ArenaBoundsCustom(Shape[] UnionShapes, Shape[]? DifferenceShapes = null, Shape[]? AdditionalShapes = null, float MapResolution = 0.5f, float ScaleFactor = 1f, bool AllowObstacleMap = false, float Offset = default, bool AdjustForHitboxInwards = false, bool AdjustForHitboxOutwards = false)
        : base(ComputeAxisExtent(All(UnionShapes, AdditionalShapes)) + HitboxMove(AdjustForHitboxInwards, AdjustForHitboxOutwards), ComputeRadius(All(UnionShapes, AdditionalShapes)) + HitboxMove(AdjustForHitboxInwards, AdjustForHitboxOutwards))
    {
        this.UnionShapes = UnionShapes;
        this.DifferenceShapes = DifferenceShapes ?? [];
        this.AdditionalShapes = AdditionalShapes ?? [];
        this.Center = ComputeCenter(All(UnionShapes, AdditionalShapes));
        this.MapResolution = MapResolution; // the caller's grid size was being accepted and dropped

        // BossmodReborn's hitbox flags: the pieces are drawn to the walls, and a character's centre stops half a yalm
        // short of a wall. Both were taken and ignored until 2026-11-21, so such a floor was half a yalm wider than
        // anyone can stand on: Forgiven Treachery's walkway reached 19 where every recorded position is within 18.54,
        // its platforms 7.5 where they stop at 7.03, and a route stepped through the notch between the two. As
        // BossmodReborn does it: the union less the differences, every edge moved, corners rounded; the additional
        // pieces are not moved.
        var move = HitboxMove(AdjustForHitboxInwards, AdjustForHitboxOutwards);
        if (move != 0f)
        {
            var (outlines, holes, islands) = MoveEdges(this.Center, UnionShapes, this.DifferenceShapes, move);
            if (outlines.Length > 0)
            {
                this.UnionShapes = outlines;
                this.DifferenceShapes = holes;
                if (islands.Length > 0)
                    this.AdditionalShapes = [.. this.AdditionalShapes, .. islands];
            }
        }
    }

    /// <summary>
    /// What <paramref name="union"/> less <paramref name="differences"/> covers with every edge moved by
    /// <paramref name="by"/> yalms (negative shrinks the floor: outlines come in and holes widen), as outlines and
    /// holes. A piece of floor standing inside a hole comes back as an island, to be added after the holes are cut.
    /// </summary>
    private static (Shape[] Outlines, Shape[] Holes, Shape[] Islands) MoveEdges(WPos center, Shape[] union, Shape[] differences, float by)
    {
        var moved = PolygonClipper.GetCombinedPolygon(center, union, differences.Length > 0 ? differences : null).Offset(by, Clipper2Lib.JoinType.Round);
        var outlines = new List<Shape>();
        var holes = new List<Shape>();
        var islands = new List<Shape>();
        var holeLoops = new List<WDir[]>();
        foreach (var part in moved.Parts)
            for (var h = 0; h < part.HoleStarts.Count; ++h)
                holeLoops.Add(part.Interior(h).ToArray());
        foreach (var part in moved.Parts)
        {
            var exterior = part.Exterior.ToArray();
            if (exterior.Length < 3)
                continue;
            var piece = new PolygonCustomRel(exterior, center);
            var inAHole = false;
            foreach (var loop in holeLoops)
                inAHole |= InLoop(loop, exterior[0]);
            (inAHole ? islands : outlines).Add(piece);
        }
        foreach (var loop in holeLoops)
            holes.Add(new PolygonCustomRel(loop, center));
        return ([.. outlines], [.. holes], [.. islands]);

        static bool InLoop(WDir[] loop, WDir p)
        {
            var inside = false;
            for (int i = 0, j = loop.Length - 1; i < loop.Length; j = i++)
            {
                if ((loop[i].Z > p.Z) != (loop[j].Z > p.Z) && p.X < ((loop[j].X - loop[i].X) * (p.Z - loop[i].Z) / (loop[j].Z - loop[i].Z)) + loop[i].X)
                    inside = !inside;
            }
            return inside;
        }
    }

    // how far the flags move the edges, and the floor's extent with them: the grid is laid out from the extent
    private static float HitboxMove(bool inwards, bool outwards) => inwards ? -HitboxRadius : outwards ? HitboxRadius : 0f;

    private static Shape[] All(Shape[] union, Shape[]? additional) => additional is { Length: > 0 } ? [.. union, .. additional] : union;

    /// <summary>
    /// The outline of what <paramref name="union"/> covers, moved by <paramref name="by"/> yalms (negative pulls it in),
    /// with the holes left where they are: the differences as given, and any hole the pieces leave between them (a
    /// ring's middle) as it is, where offsetting the whole polygon would widen it by the same amount. The outlines go
    /// through the offset together: on its own, a piece with no area has no inside to shrink into and came back bigger.
    /// </summary>
    private static (Shape[] Outlines, Shape[] Holes) MoveOutline(WPos center, Shape[] union, Shape[] differences, float by, Clipper2Lib.JoinType join)
    {
        var outlines = new List<Shape>();
        var holes = new List<Shape>(differences);
        var solid = new RelSimplifiedComplexPolygon();
        foreach (var part in PolygonClipper.GetCombinedPolygon(center, union).Parts)
        {
            solid.Parts.Add(new RelPolygonWithHoles([.. part.Exterior]));
            for (var h = 0; h < part.HoleStarts.Count; ++h)
                holes.Add(new PolygonCustomRel(part.Interior(h).ToArray(), center));
        }
        foreach (var part in solid.Offset(by, join).Parts)
        {
            outlines.Add(new PolygonCustomRel(part.Exterior.ToArray(), center));
            for (var h = 0; h < part.HoleStarts.Count; ++h)
                holes.Add(new PolygonCustomRel(part.Interior(h).ToArray(), center));
        }
        return ([.. outlines], [.. holes]);
    }

    private static WPos ComputeCenter(Shape[] shapes)
    {
        var min = new WPos(float.MaxValue, float.MaxValue);
        var max = new WPos(float.MinValue, float.MinValue);
        foreach (var s in shapes)
        {
            var (smin, smax) = Bounds(s.ContourWorld());
            min = new WPos(MathF.Min(min.X, smin.X), MathF.Min(min.Z, smin.Z));
            max = new WPos(MathF.Max(max.X, smax.X), MathF.Max(max.Z, smax.Z));
        }
        return min + (max - min) * 0.5f;
    }

    /// <inheritdoc/>
    /// <remarks>Resolved against this arena's own <see cref="Center"/>: the shapes are world-space, so the
    /// origin is not a meaningful reference for them the way it is for a circle or a rectangle.</remarks>
    public override bool Contains(in WDir offset) => this.Contains(this.Center, this.Center + offset);

    public override bool Contains(WPos center, WPos point)
    {
        foreach (var s in this.AdditionalShapes)
            if (s.Contains(point))
                return true;
        var inside = false;
        foreach (var s in this.UnionShapes)
            if (s.Contains(point)) { inside = true; break; }
        if (!inside)
            return false;
        foreach (var s in this.DifferenceShapes)
            if (s.Contains(point))
                return false;
        return true;
    }

    public override IReadOnlyList<WPos> Contour(WPos center) => LargestShape(this.UnionShapes).ContourWorld();

    /// <summary>The floor's edge pulled in, the holes left where they are. A floor built from <see cref="Polygon"/>s,
    /// which is every custom Occult Crescent arena, shrinks each about its own centre. Any other floor shrinks as one
    /// outline: shrinking its pieces one by one would cut apart pieces that only touch, as Lauda's side nubs touch the
    /// rectangle they hang off. Until 2026-11-05 such a floor came back unchanged, so a margin on it did nothing.</summary>
    public override ArenaBounds Inset(float margin)
    {
        var union = new Shape[this.UnionShapes.Length];
        var polygons = true;
        for (var i = 0; i < union.Length && polygons; ++i)
        {
            if (this.UnionShapes[i] is Polygon p)
                union[i] = new Polygon(p.Center, MathF.Max(p.Radius - margin, 0f), p.Edges, p.Rotation);
            else
                polygons = false;
        }
        if (polygons)
            return new ArenaBoundsCustom(union, this.DifferenceShapes, this.AdditionalShapes, MapResolution: this.MapResolution) { IsCircle = this.IsCircle };

        var (outlines, holes) = MoveOutline(this.Center, this.UnionShapes, this.DifferenceShapes, -margin, Clipper2Lib.JoinType.Miter);
        return outlines.Length == 0 ? this : new ArenaBoundsCustom(outlines, holes, this.AdditionalShapes, MapResolution: this.MapResolution);
    }

    public override IReadOnlyList<IReadOnlyList<WPos>> Obstacles(WPos center)
    {
        var n = this.DifferenceShapes.Length;
        if (n == 0)
            return [];
        var contours = new IReadOnlyList<WPos>[n];
        for (var i = 0; i < n; ++i)
            contours[i] = this.DifferenceShapes[i].ContourWorld();
        return contours;
    }

    private static Shape LargestShape(Shape[] shapes)
    {
        Shape best = shapes[0];
        var bestExtent = -1f;
        foreach (var s in shapes)
        {
            var (min, max) = Bounds(s.ContourWorld());
            var extent = (max - min).LengthSq();
            if (extent > bestExtent) { bestExtent = extent; best = s; }
        }
        return best;
    }

    /// <summary>
    /// True circumradius about the arena centre — the farthest any boundary point sits from it. The radar
    /// scales the canvas by this, so it has to be the real extent: half the bounding-box *diagonal*
    /// over-states a circular field by sqrt(2), rendering a 19.5y arena as though it were 27.6y and
    /// shrinking it to 71% of the canvas. A square is unaffected either way, since its farthest boundary
    /// point is the corner.
    /// </summary>
    // BossmodReborn's radius for a custom floor: the furthest its outline reaches along either axis from the centre
    private static float ComputeAxisExtent(Shape[] shapes)
    {
        var center = ComputeCenter(shapes);
        var extent = 0f;
        foreach (var s in shapes)
        {
            var contour = s.ContourWorld();
            for (var i = 0; i < contour.Count; ++i)
                extent = MathF.Max(extent, MathF.Max(MathF.Abs(contour[i].X - center.X), MathF.Abs(contour[i].Z - center.Z)));
        }
        return extent;
    }

    private static float ComputeRadius(Shape[] shapes)
    {
        var center = ComputeCenter(shapes);
        var radius = 0f;
        foreach (var s in shapes)
        {
            var contour = s.ContourWorld();
            for (var i = 0; i < contour.Count; ++i)
                radius = MathF.Max(radius, (contour[i] - center).Length());
        }
        return radius;
    }

    private static (WPos min, WPos max) Bounds(IReadOnlyList<WPos> pts)
    {
        var min = new WPos(float.MaxValue, float.MaxValue);
        var max = new WPos(float.MinValue, float.MinValue);
        foreach (var p in pts)
        {
            min = new WPos(MathF.Min(min.X, p.X), MathF.Min(min.Z, p.Z));
            max = new WPos(MathF.Max(max.X, p.X), MathF.Max(max.Z, p.Z));
        }
        return (min, max);
    }
}
