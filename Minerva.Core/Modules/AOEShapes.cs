namespace Minerva;

/// <summary>
/// A 2D danger shape, positioned by an origin + rotation. Kept purely geometric: <see cref="Check"/>
/// hit-tests a point, and <see cref="Contour"/> returns an outline polygon the renderer draws. No
/// coupling to ImGui — the plugin's radar turns contours into pixels.
/// </summary>
public abstract class AOEShape
{
    /// <summary>Angular resolution for tessellating arcs into line segments.</summary>
    protected const int ArcSegments = 40;

    public abstract bool Check(WPos position, WPos origin, Angle rotation);

    /// <summary>Closed outline of the shape in world space (for filled/outlined rendering).</summary>
    public abstract IReadOnlyList<WPos> Contour(WPos origin, Angle rotation);

    public bool Check(WPos position, Actor? origin) => origin != null && this.Check(position, origin.Position, origin.Rotation);
    public IReadOnlyList<WPos> Contour(Actor origin) => this.Contour(origin.Position, origin.Rotation);

    protected static void AddArc(List<WPos> pts, WPos center, float radius, Angle from, Angle to, int segments)
    {
        var step = (to - from).Rad / segments;
        for (var i = 0; i <= segments; ++i)
        {
            var a = new Angle(from.Rad + step * i);
            pts.Add(center + a.ToDirection() * radius);
        }
    }
}

public sealed class AOEShapeCircle(float radius) : AOEShape
{
    public readonly float Radius = radius;

    public override bool Check(WPos position, WPos origin, Angle rotation) => position.InCircle(origin, this.Radius);

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var pts = new List<WPos>(ArcSegments + 1);
        AddArc(pts, origin, this.Radius, default, new Angle(Angle.TwoPI), ArcSegments);
        return pts;
    }

    public override string ToString() => $"Circle r={this.Radius:f1}";
}

public sealed class AOEShapeDonut(float innerRadius, float outerRadius) : AOEShape
{
    public readonly float InnerRadius = innerRadius;
    public readonly float OuterRadius = outerRadius;

    public override bool Check(WPos position, WPos origin, Angle rotation) => position.InDonut(origin, this.InnerRadius, this.OuterRadius);

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        // outer ring CCW then inner ring CW, so a fill renders the ring (even-odd)
        var pts = new List<WPos>(2 * (ArcSegments + 1));
        AddArc(pts, origin, this.OuterRadius, default, new Angle(Angle.TwoPI), ArcSegments);
        AddArc(pts, origin, this.InnerRadius, new Angle(Angle.TwoPI), default, ArcSegments);
        return pts;
    }

    public override string ToString() => $"Donut {this.InnerRadius:f1}-{this.OuterRadius:f1}";
}

/// <summary>Cone from the origin: <paramref name="halfAngle"/> is half the full opening.</summary>
public sealed class AOEShapeCone(float radius, Angle halfAngle, Angle directionOffset = default) : AOEShape
{
    public readonly float Radius = radius;
    public readonly Angle HalfAngle = halfAngle;
    public readonly Angle DirectionOffset = directionOffset;

    public override bool Check(WPos position, WPos origin, Angle rotation)
        => position.InCircleCone(origin, this.Radius, rotation + this.DirectionOffset, this.HalfAngle);

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var dir = rotation + this.DirectionOffset;
        var pts = new List<WPos> { origin };
        var segs = Math.Max(2, (int)(ArcSegments * this.HalfAngle.Rad / MathF.PI));
        AddArc(pts, origin, this.Radius, dir - this.HalfAngle, dir + this.HalfAngle, segs);
        return pts;
    }

    public override string ToString() => $"Cone r={this.Radius:f1} halfAngle={this.HalfAngle}";
}

/// <summary>
/// Rectangle extending <paramref name="lenFront"/> forward and <paramref name="lenBack"/> back
/// along the facing, with <paramref name="halfWidth"/> to each side.
/// </summary>
public sealed class AOEShapeRect(float lenFront, float halfWidth, float lenBack = 0f) : AOEShape
{
    public readonly float LenFront = lenFront;
    public readonly float HalfWidth = halfWidth;
    public readonly float LenBack = lenBack;

    public override bool Check(WPos position, WPos origin, Angle rotation)
        => position.InRect(origin, rotation, this.LenFront, this.LenBack, this.HalfWidth);

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var fwd = rotation.ToDirection();
        var side = fwd.OrthoL();
        return
        [
            origin + fwd * this.LenFront + side * this.HalfWidth,
            origin + fwd * this.LenFront - side * this.HalfWidth,
            origin - fwd * this.LenBack - side * this.HalfWidth,
            origin - fwd * this.LenBack + side * this.HalfWidth,
        ];
    }

    public override string ToString() => $"Rect {this.LenFront:f1}x{this.HalfWidth * 2f:f1}";
}

/// <summary>Plus/cross shape: two perpendicular arms of half-length <paramref name="length"/>.</summary>
public sealed class AOEShapeCross(float length, float halfWidth) : AOEShape
{
    public readonly float Length = length;
    public readonly float HalfWidth = halfWidth;

    public override bool Check(WPos position, WPos origin, Angle rotation)
        => (position - origin).InCross(rotation.ToDirection(), this.Length, this.HalfWidth);

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        // 12-point plus outline
        var f = rotation.ToDirection();
        var s = f.OrthoL();
        var l = this.Length;
        var w = this.HalfWidth;
        return
        [
            origin + f * l + s * w, origin + f * l - s * w,
            origin + f * w - s * w, origin + f * w - s * l,
            origin - f * w - s * l, origin - f * w - s * w,
            origin - f * l - s * w, origin - f * l + s * w,
            origin - f * w + s * w, origin - f * w + s * l,
            origin + f * w + s * l, origin + f * w + s * w,
        ];
    }

    public override string ToString() => $"Cross {this.Length:f1}/{this.HalfWidth:f1}";
}

/// <summary>
/// An arbitrary AOE built from absolute-world <see cref="Shape"/> operands: <c>shapes1</c> combined with
/// <c>shapes2</c> via <see cref="OperandType"/>, minus <c>differenceShapes</c>. Matches BossmodReborn's
/// <c>AOEShapeCustom</c> constructor (BSD-3; see THIRD-PARTY-NOTICES.txt) so DT shape definitions paste in
/// unchanged. Containment is analytic (no clipper); the drawn outline is the first positive operand's
/// (the renderer draws a single contour). Operands are absolute, so a non-default <c>origin</c>/rotation
/// only rotates the test point about the origin (the common case is default origin + default rotation).
/// </summary>
public sealed class AOEShapeCustom : AOEShape
{
    private readonly IReadOnlyList<Shape> shapes1;
    private readonly IReadOnlyList<Shape> difference;
    private readonly IReadOnlyList<Shape> shapes2;
    private readonly OperandType operand;
    private readonly bool invert;

    public AOEShapeCustom(IReadOnlyList<Shape> shapes1, IReadOnlyList<Shape>? differenceShapes = null, IReadOnlyList<Shape>? shapes2 = null, OperandType operand = OperandType.Union, WPos origin = default, bool invertForbiddenZone = false)
    {
        this.shapes1 = shapes1;
        this.difference = differenceShapes ?? [];
        this.shapes2 = shapes2 ?? [];
        this.operand = operand;
        this.invert = invertForbiddenZone;
    }

    private static bool AnyContains(IReadOnlyList<Shape> shapes, WPos p)
    {
        for (var i = 0; i < shapes.Count; ++i)
            if (shapes[i].Contains(p))
                return true;
        return false;
    }

    public override bool Check(WPos position, WPos origin, Angle rotation)
    {
        var p = position;
        if (rotation.Rad != 0f)
        {
            var off = position - origin;
            var c = MathF.Cos(-rotation.Rad);
            var s = MathF.Sin(-rotation.Rad);
            p = origin + new WDir(off.X * c - off.Z * s, off.X * s + off.Z * c);
        }

        var inside = AnyContains(this.shapes1, p);
        if (this.shapes2.Count > 0)
        {
            var i2 = AnyContains(this.shapes2, p);
            inside = this.operand switch
            {
                OperandType.Intersection => inside && i2,
                OperandType.Xor => inside ^ i2,
                _ => inside || i2,
            };
        }
        if (inside && this.difference.Count > 0 && AnyContains(this.difference, p))
            inside = false;

        return this.invert ? !inside : inside;
    }

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
        => this.shapes1.Count > 0 ? this.shapes1[0].ContourWorld() : [];

    public override string ToString() => $"CustomAOE u={this.shapes1.Count} d={this.difference.Count}";
}
