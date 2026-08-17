namespace Minerva;

/// <summary>How a secondary shape list is combined with the primary one in <see cref="AOEShapeCustom"/>.</summary>
public enum OperandType { Union, Intersection, Xor, Difference }

/// <summary>
/// A geometric operand used to compose custom arenas / AOEs (see <see cref="ArenaBoundsCustom"/> and
/// <see cref="AOEShapeCustom"/>). Unlike <see cref="AOEShape"/> these carry absolute world positions and
/// are combined by union/difference. Ported to match BossmodReborn's <c>Shape</c> family (BSD-3; see
/// THIRD-PARTY-NOTICES.txt) so module arena definitions paste in unchanged. Minerva computes containment
/// analytically (no polygon clipper), so hit-testing is exact while the drawn outline of a boolean
/// combination is approximate (each operand is drawn separately).
/// </summary>
public abstract class Shape
{
    protected const int Segments = 60;

    /// <summary>Is the absolute world point inside this shape?</summary>
    public abstract bool Contains(WPos p);

    /// <summary>Closed outline of this shape in absolute world space (for drawing).</summary>
    public abstract IReadOnlyList<WPos> ContourWorld();

    /// <summary>BMR-compatible: outline as offsets from the given center.</summary>
    public List<WDir> Contour(WPos center)
    {
        var world = this.ContourWorld();
        var result = new List<WDir>(world.Count);
        for (var i = 0; i < world.Count; ++i)
            result.Add(world[i] - center);
        return result;
    }

    // even-odd point-in-polygon on an absolute-world vertex loop
    protected static bool InPolygon(IReadOnlyList<WPos> v, WPos p)
    {
        var inside = false;
        for (int i = 0, j = v.Count - 1; i < v.Count; j = i++)
        {
            if ((v[i].Z > p.Z) != (v[j].Z > p.Z)
                && p.X < (v[j].X - v[i].X) * (p.Z - v[i].Z) / (v[j].Z - v[i].Z) + v[i].X)
                inside = !inside;
        }
        return inside;
    }

    protected static WDir Rotate(WDir d, Angle a)
    {
        var c = MathF.Cos(a.Rad);
        var s = MathF.Sin(a.Rad);
        return new WDir(d.X * c - d.Z * s, d.X * s + d.Z * c);
    }

    protected static List<WPos> Arc(WPos center, float radius, Angle from, Angle to, int segments)
    {
        var pts = new List<WPos>(segments + 1);
        var step = (to - from).Rad / segments;
        for (var i = 0; i <= segments; ++i)
            pts.Add(center + new Angle(from.Rad + step * i).ToDirection() * radius);
        return pts;
    }
}

public sealed class Circle(WPos center, float radius) : Shape
{
    public readonly WPos Center = center;
    public readonly float Radius = radius;
    public override bool Contains(WPos p) => p.InCircle(this.Center, this.Radius);
    public override IReadOnlyList<WPos> ContourWorld() => Arc(this.Center, this.Radius, default, Angle.TwoPI.Radians(), Segments);
}

public sealed class Donut(WPos center, float innerRadius, float outerRadius) : Shape
{
    public readonly WPos Center = center;
    public readonly float InnerRadius = innerRadius;
    public readonly float OuterRadius = outerRadius;
    public override bool Contains(WPos p) => p.InDonut(this.Center, this.InnerRadius, this.OuterRadius);
    public override IReadOnlyList<WPos> ContourWorld() => Arc(this.Center, this.OuterRadius, default, Angle.TwoPI.Radians(), Segments);
}

/// <summary>A tessellated donut (regular annulus). Containment is the analytic ring.</summary>
public sealed class DonutV(WPos center, float innerRadius, float outerRadius, int edges, Angle rotation = default) : Shape
{
    public readonly WPos Center = center;
    public readonly float InnerRadius = innerRadius;
    public readonly float OuterRadius = outerRadius;
    public readonly int Edges = edges;
    public readonly Angle Rotation = rotation;
    public override bool Contains(WPos p) => p.InDonut(this.Center, this.InnerRadius, this.OuterRadius);
    public override IReadOnlyList<WPos> ContourWorld() => Arc(this.Center, this.OuterRadius, this.Rotation, this.Rotation + Angle.TwoPI.Radians(), Math.Max(this.Edges, 3));
}

public class Rectangle(WPos center, float halfWidth, float halfHeight, Angle rotation = default) : Shape
{
    public readonly WPos Center = center;
    public readonly float HalfWidth = halfWidth;
    public readonly float HalfHeight = halfHeight;
    public readonly Angle Rotation = rotation;

    public override bool Contains(WPos p)
    {
        var local = Rotate(p - this.Center, -this.Rotation);
        return MathF.Abs(local.X) <= this.HalfWidth && MathF.Abs(local.Z) <= this.HalfHeight;
    }

    public override IReadOnlyList<WPos> ContourWorld()
    {
        var w = this.HalfWidth;
        var h = this.HalfHeight;
        return
        [
            this.Center + Rotate(new WDir(-w, -h), this.Rotation),
            this.Center + Rotate(new WDir(w, -h), this.Rotation),
            this.Center + Rotate(new WDir(w, h), this.Rotation),
            this.Center + Rotate(new WDir(-w, h), this.Rotation),
        ];
    }
}

/// <summary>An axis-agnostic rectangle spanning from <paramref name="start"/> to <paramref name="end"/>.</summary>
public sealed class RectangleSE(WPos start, WPos end, float halfWidth)
    : Rectangle((start + (end - start) * 0.5f), halfWidth, (end - start).Length() * 0.5f, Angle.FromDirection(end - start));

public sealed class Square(WPos center, float halfSize, Angle rotation = default) : Rectangle(center, halfSize, halfSize, rotation);

public sealed class Cross(WPos center, float length, float halfWidth, Angle rotation = default) : Shape
{
    public readonly WPos Center = center;
    public readonly float Length = length;
    public readonly float HalfWidth = halfWidth;
    public readonly Angle Rotation = rotation;

    public override bool Contains(WPos p) => (p - this.Center).InCross(this.Rotation.ToDirection(), this.Length, this.HalfWidth);

    public override IReadOnlyList<WPos> ContourWorld()
    {
        // 12-vertex plus-sign outline
        var l = this.Length;
        var w = this.HalfWidth;
        WDir[] local =
        [
            new(w, l), new(w, w), new(l, w), new(l, -w), new(w, -w), new(w, -l),
            new(-w, -l), new(-w, -w), new(-l, -w), new(-l, w), new(-w, w), new(-w, l),
        ];
        var result = new WPos[local.Length];
        for (var i = 0; i < local.Length; ++i)
            result[i] = this.Center + Rotate(local[i], this.Rotation);
        return result;
    }
}

/// <summary>A regular polygon of <paramref name="edges"/> sides inscribed in a circle of <paramref name="radius"/>.</summary>
public sealed class Polygon(WPos center, float radius, int edges, Angle rotation = default) : Shape
{
    public readonly WPos Center = center;
    public readonly float Radius = radius;
    public readonly int Edges = edges;
    public readonly Angle Rotation = rotation;

    private WPos[] Vertices()
    {
        var n = Math.Max(this.Edges, 3);
        var pts = new WPos[n];
        var step = Angle.TwoPI / n;
        for (var i = 0; i < n; ++i)
            pts[i] = this.Center + new Angle(this.Rotation.Rad + step * i).ToDirection() * this.Radius;
        return pts;
    }

    public override bool Contains(WPos p) => InPolygon(this.Vertices(), p);
    public override IReadOnlyList<WPos> ContourWorld() => this.Vertices();
}

/// <summary>A polygon defined by explicit absolute vertices.</summary>
public sealed class PolygonCustom(WPos[] vertices) : Shape
{
    public readonly WPos[] Vertices = vertices;
    public override bool Contains(WPos p) => InPolygon(this.Vertices, p);
    public override IReadOnlyList<WPos> ContourWorld() => this.Vertices;
}

/// <summary>A circular sector from <paramref name="startAngle"/> to <paramref name="endAngle"/>.</summary>
public class Cone(WPos center, float radius, Angle startAngle, Angle endAngle) : Shape
{
    public readonly WPos Center = center;
    public readonly float Radius = radius;
    public readonly Angle StartAngle = startAngle;
    public readonly Angle EndAngle = endAngle;

    public override bool Contains(WPos p)
    {
        if (!p.InCircle(this.Center, this.Radius))
            return false;
        var ang = Angle.FromDirection(p - this.Center);
        var half = (this.EndAngle - this.StartAngle).Rad * 0.5f;
        var mid = new Angle(this.StartAngle.Rad + half);
        return MathF.Abs((ang - mid).Normalized().Rad) <= MathF.Abs(half);
    }

    public override IReadOnlyList<WPos> ContourWorld()
    {
        var pts = new List<WPos> { this.Center };
        pts.AddRange(Arc(this.Center, this.Radius, this.StartAngle, this.EndAngle, Segments));
        return pts;
    }
}

/// <summary>A cone specified by a center direction and half-angle.</summary>
public sealed class ConeHA(WPos center, float radius, Angle centerDir, Angle halfAngle)
    : Cone(center, radius, centerDir - halfAngle, centerDir + halfAngle);
