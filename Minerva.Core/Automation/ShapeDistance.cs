namespace Minerva;

/// <summary>
/// A signed-distance function over the XZ plane: <see cref="Distance"/> is negative inside the shape and
/// positive outside, so a value ≤ 0 means "inside / forbidden". This is the currency the auto-dodge
/// engine's forbidden zones are expressed in. Ported to match BossmodReborn's <c>ShapeDistance</c> family
/// (BSD-3; see THIRD-PARTY-NOTICES.txt); the "Inverted" variants forbid the complement (you must stay in).
/// </summary>
public abstract class ShapeDistance
{
    public abstract float Distance(WPos p);

    /// <summary>True when the point is inside/forbidden (distance ≤ 0).</summary>
    public bool Contains(WPos p) => this.Distance(p) <= 0f;
}

/// <summary>
/// Signed distance for a boolean combination of <see cref="Shape"/> operands — the field a line-of-sight
/// safe zone needs. The boolean-only fallback (<see cref="SDShapeCheck"/>) answers "are you safe" with ±1,
/// which tells the auto-dodge that it is standing somewhere lethal but nothing about which way cover lies;
/// on a hide-behind-the-rock mechanic a flat field is the difference between reaching a boulder and dying
/// next to one. Distance is taken to each operand's contour, so it is exact to the polygon approximation
/// the shapes already use for hit-testing. Contours are captured once here, not per query — the operands
/// are fixed when a safe zone is built, and the pathfinder samples this thousands of times per step.
/// </summary>
public sealed class SDShapeSet : ShapeDistance
{
    private readonly Shape[] union;
    private readonly Shape[] difference;
    private readonly bool invert;

    public SDShapeSet(IReadOnlyList<Shape> union, IReadOnlyList<Shape> difference, bool invert)
    {
        this.union = [.. union];
        this.difference = [.. difference];
        this.invert = invert;
    }

    public override float Distance(WPos p)
    {
        if (this.union.Length == 0)
            return this.invert ? -1f : 1f;

        // union: inside if inside any operand, so the nearest boundary wins
        var d = float.MaxValue;
        for (var i = 0; i < this.union.Length; ++i)
            d = MathF.Min(d, this.union[i].SignedDistance(p));

        // difference: also required to be outside each subtracted operand
        for (var i = 0; i < this.difference.Length; ++i)
            d = MathF.Max(d, -this.difference[i].SignedDistance(p));

        // negative means "inside the combination"; an inverted zone marks the safe ground, so flip it to
        // keep the convention the auto-dodge reads — negative is always the place you must not be
        return this.invert ? -d : d;
    }

}

public sealed class SDCircle(WPos origin, float radius) : ShapeDistance
{
    public override float Distance(WPos p) => (p - origin).Length() - radius;
}

public sealed class SDInvertedCircle(WPos origin, float radius) : ShapeDistance
{
    public override float Distance(WPos p) => radius - (p - origin).Length();
}

public sealed class SDDonut(WPos origin, float innerRadius, float outerRadius) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var d = (p - origin).Length();
        return MathF.Max(innerRadius - d, d - outerRadius); // ≤0 within the ring
    }
}

/// <summary>Inverse of <see cref="SDDonut"/>: safe inside the ring, forbidden everywhere else.</summary>
public sealed class SDInvertedDonut(WPos origin, float innerRadius, float outerRadius) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var d = (p - origin).Length();
        return -MathF.Max(innerRadius - d, d - outerRadius);
    }
}

public sealed class SDRect : ShapeDistance
{
    private readonly WPos origin;
    private readonly WDir dir;      // forward
    private readonly WDir normal;   // left
    private readonly float lenFront, lenBack, halfWidth;

    public SDRect(WPos origin, WDir direction, float lenFront, float lenBack, float halfWidth)
    {
        this.origin = origin;
        this.dir = direction;
        this.normal = direction.OrthoL();
        this.lenFront = lenFront;
        this.lenBack = lenBack;
        this.halfWidth = halfWidth;
    }

    public SDRect(WPos origin, Angle direction, float lenFront, float lenBack, float halfWidth)
        : this(origin, direction.ToDirection(), lenFront, lenBack, halfWidth) { }

    public SDRect(WPos from, WPos to, float halfWidth)
        : this(from, (to - from).Normalized(), (to - from).Length(), 0f, halfWidth) { }

    public override float Distance(WPos p)
    {
        var off = p - this.origin;
        var fwd = off.Dot(this.dir);
        var side = off.Dot(this.normal);
        var dFwd = MathF.Max(fwd - this.lenFront, -this.lenBack - fwd);
        var dSide = MathF.Abs(side) - this.halfWidth;

        // max() alone is the inside-only form: correct within the rectangle and along its faces, but short
        // of the truth past a corner, where it returns the larger leg instead of the hypotenuse (3 and 4
        // out reads as 4 away, not 5). Understating the distance makes the dodge treat safe ground near a
        // corner as marginal, so take the real diagonal outside and keep max() for within.
        if (dFwd <= 0f || dSide <= 0f)
            return MathF.Max(dFwd, dSide);
        return MathF.Sqrt((dFwd * dFwd) + (dSide * dSide));
    }
}

public sealed class SDInvertedRect(WPos origin, WDir direction, float lenFront, float lenBack, float halfWidth) : ShapeDistance
{
    private readonly SDRect inner = new(origin, direction, lenFront, lenBack, halfWidth);
    public SDInvertedRect(WPos origin, Angle direction, float lenFront, float lenBack, float halfWidth) : this(origin, direction.ToDirection(), lenFront, lenBack, halfWidth) { }
    public SDInvertedRect(WPos from, WPos to, float halfWidth) : this(from, (to - from).Normalized(), (to - from).Length(), 0f, halfWidth) { }
    public override float Distance(WPos p) => -this.inner.Distance(p);
}

/// <summary>
/// Distance to a circular sector — the apex, its two straight edges, and the arc closing them.
/// <para>The obvious formulation, <c>max(radialDistance, angularOffset)</c>, has the right sign but no
/// meaning in its magnitude: it takes the larger of a length in yalms and an angle in radians. Sign is all
/// a containment test needs, which is why it survived, but every consumer that reads the number — the
/// clearance margin, the escape cost behind a cast budget — was being handed a units error.</para>
/// <para>A sector is an annular sector with no hole, so the two share their arithmetic.</para>
/// </summary>
public sealed class SDCone(WPos origin, float radius, Angle centerDir, Angle halfAngle) : ShapeDistance
{
    public override float Distance(WPos p) => SectorDistance.Signed(p, origin, 0f, radius, centerDir, halfAngle);
}

/// <summary>Distance to an annular sector: between two radii and within ±halfAngle of a direction.</summary>
public sealed class SDDonutSector(WPos origin, float innerRadius, float outerRadius, Angle centerDir, Angle halfAngle) : ShapeDistance
{
    public override float Distance(WPos p) => SectorDistance.Signed(p, origin, innerRadius, outerRadius, centerDir, halfAngle);
}

/// <summary>Shared exact geometry for sectors and annular sectors.</summary>
internal static class SectorDistance
{
    public static float Signed(WPos p, WPos origin, float inner, float outer, Angle centerDir, Angle halfAngle)
    {
        var h = MathF.Abs(halfAngle.Rad);
        var off = p - origin;
        var d = off.Length();

        // a half-angle of pi or more leaves no wedge out: it is the full ring, and the radial edges the
        // sector arithmetic leans on have collapsed onto each other
        if (h >= MathF.PI - 1e-4f)
            return inner > 0f ? MathF.Max(inner - d, d - outer) : d - outer;

        var a = d > 1e-5f ? MathF.Abs((Angle.FromDirection(off) - centerDir).Normalized().Rad) : 0f;
        var withinArc = a <= h;

        // the two radial edges, each a segment from the inner radius out to the outer
        var u1 = (centerDir + new Angle(h)).ToDirection();
        var u2 = (centerDir - new Angle(h)).ToDirection();
        var edge = MathF.Min(
            ToSegment(p, origin + (u1 * inner), origin + (u1 * outer)),
            ToSegment(p, origin + (u2 * inner), origin + (u2 * outer)));

        if (withinArc && d >= inner && d <= outer)
            return -Min3(outer - d, d - inner, edge);           // inside: nearest way out

        if (!withinArc)
            return edge;                                        // in the missing wedge: only the edges are near

        return MathF.Min(d > outer ? d - outer : inner - d, edge);
    }

    private static float Min3(float a, float b, float c) => MathF.Min(a, MathF.Min(b, c));

    private static float ToSegment(WPos p, WPos a, WPos b)
    {
        var ab = b - a;
        var lenSq = ab.LengthSq();
        var t = lenSq > 1e-6f ? Math.Clamp((p - a).Dot(ab) / lenSq, 0f, 1f) : 0f;
        return (p - (a + (ab * t))).Length();
    }
}

public sealed class SDInvertedCone(WPos origin, float radius, Angle centerDir, Angle halfAngle) : ShapeDistance
{
    private readonly SDCone inner = new(origin, radius, centerDir, halfAngle);
    public override float Distance(WPos p) => -this.inner.Distance(p);
}

public sealed class SDCapsule : ShapeDistance
{
    private readonly WPos a, b;
    private readonly float radius;

    public SDCapsule(WPos origin, WDir direction, float length, float radius)
    {
        this.a = origin;
        this.b = origin + direction.Normalized() * length;
        this.radius = radius;
    }

    public SDCapsule(WPos origin, Angle direction, float length, float radius) : this(origin, direction.ToDirection(), length, radius) { }

    public override float Distance(WPos p)
    {
        var ab = this.b - this.a;
        var t = ab.LengthSq() > 0f ? Math.Clamp((p - this.a).Dot(ab) / ab.LengthSq(), 0f, 1f) : 0f;
        return (p - (this.a + ab * t)).Length() - this.radius;
    }
}

/// <summary>Intersection: the point is inside only when inside every zone (max of the distances).</summary>
public sealed class SDIntersection(ShapeDistance[] zones) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var d = float.MinValue;
        foreach (var z in zones)
            d = MathF.Max(d, z.Distance(p));
        return d;
    }
}

/// <summary>Union: the point is inside when inside any zone (min of the distances).</summary>
public sealed class SDUnion(ShapeDistance[] zones) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var d = float.MaxValue;
        foreach (var z in zones)
            d = MathF.Min(d, z.Distance(p));
        return d;
    }
}

/// <summary>Forbids everything outside the union of the given zones (you must stay within one of them).</summary>
public sealed class SDOutsideOfUnion(ShapeDistance[] zones) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var d = float.MaxValue;
        foreach (var z in zones)
            d = MathF.Min(d, z.Distance(p));
        return -d;
    }
}

/// <summary>Negates another signed distance (forbids its complement).</summary>
public sealed class SDInverted(ShapeDistance inner) : ShapeDistance
{
    public override float Distance(WPos p) => -inner.Distance(p);
}

/// <summary>Wraps an <see cref="AOEShape"/> as a boolean signed distance (±1) for shapes without an
/// analytic SDF — enough for containment-based avoidance.</summary>
public sealed class SDShapeCheck(AOEShape shape, WPos origin, Angle rotation) : ShapeDistance
{
    public override float Distance(WPos p) => shape.Check(p, origin, rotation) ? -1f : 1f;
}

// Knockback-resolution SDFs. BMR computes the post-knockback safe spot; Minerva has no knockback
// pathfinding, so these are safe "nothing forbidden" stubs (Distance always positive) that keep ported
// knockback modules compiling and still showing their AOE visuals. Args intentionally unused.
#pragma warning disable CS9113 // parameter is unread (intentional stub)
public sealed class SDKnockbackInAABBSquareAwayFromOrigin(WPos center, WPos origin, float distance, float halfWidth) : ShapeDistance
{ public override float Distance(WPos p) => 1f; }
public sealed class SDKnockbackInCircleLeftRightAlongZAxis(WPos center, float distance, float radius) : ShapeDistance
{ public override float Distance(WPos p) => 1f; }
public sealed class SDKnockbackInCircleLeftRightAlongXAxis(WPos center, float distance, float radius) : ShapeDistance
{ public override float Distance(WPos p) => 1f; }
public sealed class SDKnockbackInAABBSquareAwayFromOriginPlusAOECirclesMixedRadii(WPos center, WPos origin, float distance, float halfWidth, (WPos Origin, float Radius)[] aoes, int length) : ShapeDistance
{ public override float Distance(WPos p) => 1f; }
public sealed class SDKnockbackInAABBSquareAwayFromOriginPlusAOECirclesMixedRadiiPlusAvoidShape(WPos center, WPos origin, float distance, float halfWidth, (WPos Origin, float Radius)[] aoes, int length, ShapeDistance shape) : ShapeDistance
{ public override float Distance(WPos p) => 1f; }
#pragma warning restore CS9113
