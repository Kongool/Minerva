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
        return MathF.Max(dFwd, dSide);
    }
}

public sealed class SDInvertedRect(WPos origin, WDir direction, float lenFront, float lenBack, float halfWidth) : ShapeDistance
{
    private readonly SDRect inner = new(origin, direction, lenFront, lenBack, halfWidth);
    public SDInvertedRect(WPos origin, Angle direction, float lenFront, float lenBack, float halfWidth) : this(origin, direction.ToDirection(), lenFront, lenBack, halfWidth) { }
    public SDInvertedRect(WPos from, WPos to, float halfWidth) : this(from, (to - from).Normalized(), (to - from).Length(), 0f, halfWidth) { }
    public override float Distance(WPos p) => -this.inner.Distance(p);
}

public sealed class SDCone(WPos origin, float radius, Angle centerDir, Angle halfAngle) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var off = p - origin;
        var distToOrigin = off.Length() - radius;                      // ≤0 within radius
        var angleOff = MathF.Abs((Angle.FromDirection(off) - centerDir).Normalized().Rad) - MathF.Abs(halfAngle.Rad); // ≤0 within sector
        return MathF.Max(distToOrigin, angleOff);
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
