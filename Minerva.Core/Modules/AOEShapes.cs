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
