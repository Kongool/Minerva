namespace Minerva;

/// <summary>
/// The playable extent of an arena, centered at the module's arena center. Drives the radar's
/// world→screen scale (via <see cref="Radius"/>), draws the boundary, and answers whether a point
/// is inside the field.
/// </summary>
public abstract class ArenaBounds(float radius)
{
    /// <summary>Half-extent used to scale the radar; the circumscribing radius of the shape.</summary>
    public readonly float Radius = radius;

    public abstract bool Contains(WPos center, WPos point);

    /// <summary>
    /// Distance from <paramref name="origin"/> along <paramref name="dir"/> to the boundary. Solved
    /// generically off <see cref="Contains"/> — march out to find the first outside sample, then bisect —
    /// so it works for every bounds shape including custom polygons, at the cost of being approximate
    /// (~1cm). Returns 0 if the origin is already outside.
    /// </summary>
    public float IntersectRay(WPos center, WPos origin, WDir dir)
    {
        if (!this.Contains(center, origin))
            return 0f;
        var far = this.Radius * 2f + 1f;
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
}

public sealed class ArenaBoundsSquare(float halfWidth) : ArenaBounds(halfWidth * 1.41421356f)
{
    public readonly float HalfWidth = halfWidth;

    public override bool Contains(WPos center, WPos point)
        => MathF.Abs(point.X - center.X) <= this.HalfWidth && MathF.Abs(point.Z - center.Z) <= this.HalfWidth;

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
    : ArenaBounds(MathF.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight))
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
public sealed class ArenaBoundsPolygon(params WDir[] offsets) : ArenaBounds(MaxRadiusOf(offsets))
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
}

/// <summary>
/// A mesh arena composed of absolute-world <see cref="Shape"/>s: the field is inside any of the
/// <c>UnionShapes</c> and outside every <c>DifferenceShape</c> (holes/pits). Matches BossmodReborn's
/// <c>ArenaBoundsCustom</c> constructor (BSD-3; see THIRD-PARTY-NOTICES.txt) so DT arena definitions paste
/// in unchanged. Containment is exact (analytic per shape); Minerva has no polygon clipper, so the drawn
/// boundary is the largest union operand's outline (the difference operands draw as an inner contour).
/// Because the operands carry absolute coordinates, the arena center passed by the module is ignored here.
/// </summary>
public sealed class ArenaBoundsCustom : ArenaBounds
{
    public readonly Shape[] UnionShapes;
    public readonly Shape[] DifferenceShapes;

    /// <summary>Bounding-box centre of the union operands — what modules pass as the arena centre.</summary>
    public readonly WPos Center;

    public ArenaBoundsCustom(Shape[] UnionShapes, Shape[]? DifferenceShapes = null, Shape[]? AdditionalShapes = null, float MapResolution = 0.5f, float ScaleFactor = 1f, bool AllowObstacleMap = false, float Offset = default, bool AdjustForHitboxInwards = false, bool AdjustForHitboxOutwards = false)
        : base(ComputeRadius(UnionShapes))
    {
        this.UnionShapes = UnionShapes;
        this.DifferenceShapes = DifferenceShapes ?? [];
        this.Center = ComputeCenter(UnionShapes);
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

    public override bool Contains(WPos center, WPos point)
    {
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
