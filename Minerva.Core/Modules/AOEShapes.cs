namespace Minerva;

/// <summary>
/// A 2D danger shape, positioned by an origin + rotation. Kept purely geometric: <see cref="Check"/>
/// hit-tests a point, and <see cref="Contour"/> returns an outline polygon the renderer draws. No
/// coupling to ImGui — the plugin's radar turns contours into pixels.
/// </summary>
public abstract class AOEShape(bool invertForbiddenZone = false)
{
    /// <summary>Angular resolution for tessellating arcs into line segments.</summary>
    protected const int ArcSegments = 40;

    /// <summary>When set, the shape marks the SAFE ground and everything outside it is forbidden.</summary>
    public bool InvertForbiddenZone = invertForbiddenZone;

    public abstract bool Check(WPos position, WPos origin, Angle rotation);

    /// <summary>Closed outline of the shape in world space (for filled/outlined rendering).</summary>
    public abstract IReadOnlyList<WPos> Contour(WPos origin, Angle rotation);

    /// <summary>
    /// Every closed loop this shape is made of. Almost all shapes are one loop, but a boolean combination is
    /// a *set* of them — a line-of-sight safe zone is one shadow per blocker — and a single-contour accessor
    /// silently reduces thirteen safe wedges to one. Anything drawing a shape should walk this, not
    /// <see cref="Contour"/>.
    /// </summary>
    public virtual IReadOnlyList<IReadOnlyList<WPos>> Contours(WPos origin, Angle rotation)
        => [this.Contour(origin, rotation)];

    public bool Check(WPos position, Actor? origin) => origin != null && this.Check(position, origin.Position, origin.Rotation);
    public IReadOnlyList<WPos> Contour(Actor origin) => this.Contour(origin.Position, origin.Rotation);

    /// <summary>Signed-distance form of this shape for the auto-dodge engine. Analytic where a shape
    /// overrides it, else a boolean (±1) fallback via <see cref="SDShapeCheck"/>.</summary>
    public virtual ShapeDistance Distance(WPos origin, Angle rotation) => new SDShapeCheck(this, origin, rotation);
    public virtual ShapeDistance InvertedDistance(WPos origin, Angle rotation) => new SDInverted(this.Distance(origin, rotation));
    public ShapeDistance Distance(Actor origin) => this.Distance(origin.Position, origin.Rotation);

    // draw helpers so ported components that call shape.Draw/Outline compile (they route through Arena)
    public void Draw(Arena arena, WPos origin, Angle rotation = default, uint color = default) => arena.ZoneShape(this, origin, rotation, color);
    public void Draw(Arena arena, Actor origin, uint color = default) => arena.ZoneShape(this, origin.Position, origin.Rotation, color);
    public void Outline(Arena arena, WPos origin, Angle rotation = default, uint color = default, float thickness = 1f) => arena.OutlineShape(this, origin, rotation, color, thickness);
    public void Outline(Arena arena, Actor origin, uint color = default, float thickness = 1f) => arena.OutlineShape(this, origin.Position, origin.Rotation, color, thickness);

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

public sealed class AOEShapeCircle(float radius, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{
    public readonly float Radius = radius;

    public override bool Check(WPos position, WPos origin, Angle rotation = default) => position.InCircle(origin, this.Radius);

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var pts = new List<WPos>(ArcSegments + 1);
        AddArc(pts, origin, this.Radius, default, new Angle(Angle.TwoPI), ArcSegments);
        return pts;
    }

    public override ShapeDistance Distance(WPos origin, Angle rotation) => new SDCircle(origin, this.Radius);
    public override ShapeDistance InvertedDistance(WPos origin, Angle rotation) => new SDInvertedCircle(origin, this.Radius);

    public override string ToString() => $"Circle r={this.Radius:f1}";
}

public sealed class AOEShapeDonut(float innerRadius, float outerRadius, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{

    public readonly float InnerRadius = innerRadius;
    public readonly float OuterRadius = outerRadius;

    public override bool Check(WPos position, WPos origin, Angle rotation = default) => position.InDonut(origin, this.InnerRadius, this.OuterRadius);

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        // outer ring CCW then inner ring CW, so a fill renders the ring (even-odd)
        var pts = new List<WPos>(2 * (ArcSegments + 1));
        AddArc(pts, origin, this.OuterRadius, default, new Angle(Angle.TwoPI), ArcSegments);
        AddArc(pts, origin, this.InnerRadius, new Angle(Angle.TwoPI), default, ArcSegments);
        return pts;
    }

    public override ShapeDistance Distance(WPos origin, Angle rotation) => new SDDonut(origin, this.InnerRadius, this.OuterRadius);
    public override ShapeDistance InvertedDistance(WPos origin, Angle rotation) => new SDInvertedDonut(origin, this.InnerRadius, this.OuterRadius);

    public override string ToString() => $"Donut {this.InnerRadius:f1}-{this.OuterRadius:f1}";
}

/// <summary>Cone from the origin: <paramref name="halfAngle"/> is half the full opening.</summary>
public sealed class AOEShapeCone(float radius, Angle halfAngle, Angle directionOffset = default, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
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

    public override ShapeDistance Distance(WPos origin, Angle rotation)
        => new SDCone(origin, this.Radius, rotation + this.DirectionOffset, this.HalfAngle);

    public override ShapeDistance InvertedDistance(WPos origin, Angle rotation)
        => new SDInvertedCone(origin, this.Radius, rotation + this.DirectionOffset, this.HalfAngle);

    public override string ToString() => $"Cone r={this.Radius:f1} halfAngle={this.HalfAngle}";
}

/// <summary>
/// Rectangle extending <paramref name="lenFront"/> forward and <paramref name="lenBack"/> back
/// along the facing, with <paramref name="halfWidth"/> to each side.
/// </summary>
/// <summary>An annulus sector: between inner and outer radius and within ±halfAngle of the facing.</summary>
public sealed class AOEShapeDonutSector(float innerRadius, float outerRadius, Angle halfAngle, Angle directionOffset = default, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{
    public readonly float InnerRadius = innerRadius;
    public readonly float OuterRadius = outerRadius;
    public readonly Angle HalfAngle = halfAngle;
    public readonly Angle DirectionOffset = directionOffset;

    public override bool Check(WPos position, WPos origin, Angle rotation)
    {
        if (!position.InDonut(origin, this.InnerRadius, this.OuterRadius))
            return false;
        var center = rotation + this.DirectionOffset;
        var off = MathF.Abs((Angle.FromDirection(position - origin) - center).Normalized().Rad);
        return off <= MathF.Abs(this.HalfAngle.Rad);
    }

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var center = rotation + this.DirectionOffset;
        var from = center - this.HalfAngle;
        var to = center + this.HalfAngle;
        var pts = new List<WPos>();
        AddArc(pts, origin, this.OuterRadius, from, to, ArcSegments);
        var inner = new List<WPos>();
        AddArc(inner, origin, this.InnerRadius, from, to, ArcSegments);
        for (var i = inner.Count - 1; i >= 0; --i)
            pts.Add(inner[i]);
        return pts;
    }

    public override ShapeDistance Distance(WPos origin, Angle rotation)
        => new SDDonutSector(origin, this.InnerRadius, this.OuterRadius, rotation + this.DirectionOffset, this.HalfAngle);

    public override string ToString() => $"DonutSector {this.InnerRadius:f1}/{this.OuterRadius:f1}";
}

/// <summary>
/// A circle swept along an arc — an orbiting AOE. <paramref name="orbitCenter"/> is what it revolves
/// around, the origin passed to <see cref="Check"/> is where the circle starts, and
/// <paramref name="angularLength"/> is how far round it travels (signed: negative goes the other way).
///
/// <para>Distinct from a donut sector, which is the static wedge the sweep passes through. The capsule has
/// round ends and does not reach the wedge's corners, so a player standing just outside an end cap is safe
/// where the sector would call them hit.</para>
/// </summary>
public sealed class AOEShapeArcCapsule(float radius, Angle angularLength, WPos orbitCenter, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{
    public readonly float Radius = radius;
    public readonly Angle AngularLength = angularLength;
    public readonly WPos OrbitCenter = orbitCenter;

    public override bool Check(WPos position, WPos origin, Angle rotation)
        => new SDArcCapsule(origin, this.OrbitCenter, this.AngularLength, this.Radius).Distance(position) <= 0f;

    public override ShapeDistance Distance(WPos origin, Angle rotation)
        => this.InvertForbiddenZone
            ? new SDInvertedArcCapsule(origin, this.OrbitCenter, this.AngularLength, this.Radius)
            : new SDArcCapsule(origin, this.OrbitCenter, this.AngularLength, this.Radius);

    public override ShapeDistance InvertedDistance(WPos origin, Angle rotation)
        => new SDInvertedArcCapsule(origin, this.OrbitCenter, this.AngularLength, this.Radius);

    /// <summary>
    /// The outline: out along the far edge of the sweep, round the end cap, back along the near edge, round
    /// the start cap. Drawn as one closed loop so the renderer fills it as a single convexity-agnostic
    /// polygon.
    /// </summary>
    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var r0 = origin - this.OrbitCenter;
        var orbitRadius = r0.Length();
        if (orbitRadius <= 0f)
            return [];

        var from = Angle.FromDirection(r0);
        var to = from + this.AngularLength;
        var outer = orbitRadius + this.Radius;
        var inner = MathF.Max(orbitRadius - this.Radius, 0f);

        // The caps bulge along the direction of travel, which flips with the sign of the sweep. Drawn as a
        // fixed +180 degree range they are right for one direction and fold back through the body for the
        // other, and a folded outline fills as a wedge -- Lost on the Wind's counter-clockwise winds.
        var half = this.AngularLength.Rad >= 0f ? 180f.Degrees() : -180f.Degrees();
        var endCap = this.OrbitCenter + r0.Rotate(this.AngularLength);
        List<WPos> pts = [];
        AddArc(pts, this.OrbitCenter, outer, from, to, ArcSegments);
        AddArc(pts, endCap, this.Radius, to, to + half, ArcSegments / 2);
        AddArc(pts, this.OrbitCenter, inner, to, from, ArcSegments);
        AddArc(pts, origin, this.Radius, from + 180f.Degrees(), from + 180f.Degrees() + half, ArcSegments / 2);
        return pts;
    }

    public override string ToString() => $"ArcCapsule: radius={this.Radius:f3}, length={this.AngularLength}, orbit={this.OrbitCenter}";
}

public sealed class AOEShapeRect(float lenFront, float halfWidth, float lenBack = 0f, Angle directionOffset = default, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{
    public readonly float LenFront = lenFront;
    public readonly float HalfWidth = halfWidth;
    public readonly float LenBack = lenBack;

    /// <summary>BossmodReborn's spelling of <see cref="LenFront"/> / <see cref="LenBack"/>.</summary>
    public float LengthFront => this.LenFront;

    public float LengthBack => this.LenBack;
    public readonly Angle DirectionOffset = directionOffset;

    public override bool Check(WPos position, WPos origin, Angle rotation)
        => position.InRect(origin, rotation + this.DirectionOffset, this.LenFront, this.LenBack, this.HalfWidth) ^ this.InvertForbiddenZone;

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var fwd = (rotation + this.DirectionOffset).ToDirection();
        var side = fwd.OrthoL();
        return
        [
            origin + fwd * this.LenFront + side * this.HalfWidth,
            origin + fwd * this.LenFront - side * this.HalfWidth,
            origin - fwd * this.LenBack - side * this.HalfWidth,
            origin - fwd * this.LenBack + side * this.HalfWidth,
        ];
    }

    public override ShapeDistance Distance(WPos origin, Angle rotation)
    {
        var dir = rotation + this.DirectionOffset;
        return this.InvertForbiddenZone
            ? new SDInvertedRect(origin, dir, this.LenFront, this.LenBack, this.HalfWidth)
            : new SDRect(origin, dir, this.LenFront, this.LenBack, this.HalfWidth);
    }

    public override string ToString() => $"Rect {this.LenFront:f1}x{this.HalfWidth * 2f:f1}";
}

/// <summary>Plus/cross shape: two perpendicular arms of half-length <paramref name="length"/>.</summary>
public sealed class AOEShapeCross(float length, float halfWidth, Angle directionOffset = default, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{
    public readonly float Length = length;
    public readonly float HalfWidth = halfWidth;

    /// <summary>Rotation added to the cast's own facing — a cross cast at 45 degrees to the boss.</summary>
    public readonly Angle DirectionOffset = directionOffset;

    public override bool Check(WPos position, WPos origin, Angle rotation)
        => (position - origin).InCross((rotation + this.DirectionOffset).ToDirection(), this.Length, this.HalfWidth);

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

    /// <summary>Two crossed bars. Outside the cross the union is exact; within the overlap at the centre it
    /// reports the nearer bar's edge, which understates the depth — the safe direction to be wrong in.</summary>
    public override ShapeDistance Distance(WPos origin, Angle rotation)
        => new SDUnion(
        [
            new SDRect(origin, rotation, this.Length, this.Length, this.HalfWidth),
            new SDRect(origin, rotation + 90f.Degrees(), this.Length, this.Length, this.HalfWidth),
        ]);

    public override string ToString() => $"Cross {this.Length:f1}/{this.HalfWidth:f1}";
}

/// <summary>A triangular cone (straight edges) of the given side length and half-angle, approximated as a
/// sector for containment. Ported to match BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public sealed class AOEShapeTriCone(float sideLength, Angle halfAngle, Angle directionOffset = default, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{
    public readonly float SideLength = sideLength;
    public readonly Angle HalfAngle = halfAngle;
    public readonly Angle DirectionOffset = directionOffset;
    private AOEShapeCone Cone => new(this.SideLength, this.HalfAngle, this.DirectionOffset);

    public override bool Check(WPos position, WPos origin, Angle rotation) => this.Cone.Check(position, origin, rotation);
    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation) => this.Cone.Contour(origin, rotation);
    public override ShapeDistance Distance(WPos origin, Angle rotation) => this.Cone.Distance(origin, rotation);
    public override string ToString() => $"TriCone {this.SideLength:f1}";
}

/// <summary>A capsule AOE: a <paramref name="length"/>-long stadium of radius <paramref name="radius"/>
/// extending forward from the origin. Ported to match BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).</summary>
public sealed class AOEShapeCapsule(float radius, float length, Angle directionOffset = default, bool invertForbiddenZone = false) : AOEShape(invertForbiddenZone)
{
    public readonly float Radius = radius;
    public readonly float Length = length;
    public readonly Angle DirectionOffset = directionOffset;

    public override bool Check(WPos position, WPos origin, Angle rotation)
    {
        var dir = (rotation + this.DirectionOffset).ToDirection();
        var ab = dir * this.Length;
        var t = Math.Clamp((position - origin).Dot(ab) / ab.LengthSq(), 0f, 1f);
        return (position - (origin + ab * t)).Length() <= this.Radius;
    }

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
    {
        var fwd = rotation + this.DirectionOffset;
        var a = origin;
        var b = origin + fwd.ToDirection() * this.Length;
        var half = MathF.PI * 0.5f;
        var pts = new List<WPos>();
        for (var i = 0; i <= ArcSegments / 2; ++i)
            pts.Add(b + new Angle(fwd.Rad - half + MathF.PI * i / (ArcSegments / 2)).ToDirection() * this.Radius);
        for (var i = 0; i <= ArcSegments / 2; ++i)
            pts.Add(a + new Angle(fwd.Rad + half + MathF.PI * i / (ArcSegments / 2)).ToDirection() * this.Radius);
        return pts;
    }

    public override ShapeDistance Distance(WPos origin, Angle rotation)
        => new SDCapsule(origin, rotation + this.DirectionOffset, this.Length, this.Radius);

    public override string ToString() => $"Capsule {this.Radius:f1}x{this.Length:f1}";
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

    /// <summary>
    /// BMR-shaped overload taking the arena centre first. BMR uses it to build its clipped polygon in
    /// arena-relative space; Minerva evaluates the shapes directly in world space, so the centre is
    /// accepted for source compatibility and otherwise unused.
    /// </summary>
    public AOEShapeCustom(WPos arenaCenter, IReadOnlyList<Shape> shapes1, IReadOnlyList<Shape>? differenceShapes = null, IReadOnlyList<Shape>? shapes2 = null, OperandType operand = OperandType.Union, bool invertForbiddenZone = false, bool skipPolygonInit = false)
        : this(shapes1, differenceShapes, shapes2, operand, arenaCenter, invertForbiddenZone) { }

    public AOEShapeCustom(IReadOnlyList<Shape> shapes1, IReadOnlyList<Shape>? differenceShapes = null, IReadOnlyList<Shape>? shapes2 = null, OperandType operand = OperandType.Union, WPos origin = default, bool invertForbiddenZone = false)
    {
        this.shapes1 = shapes1;
        this.difference = differenceShapes ?? [];
        this.shapes2 = shapes2 ?? [];
        this.operand = operand;
        this.origin = origin;
        this.InvertForbiddenZone = invertForbiddenZone;
    }

    private readonly WPos origin;
    private RelSimplifiedComplexPolygon? polygon;
    private WPos polygonOrigin;
    private bool polygonReplaced;

    /// <summary>
    /// The shapes clipped into one polygon, in arena-relative space — BossmodReborn's representation.
    ///
    /// <para>Built on first use rather than in the constructor. Minerva evaluates <see cref="Check"/> by
    /// testing the source shapes directly, which needs no clipping at all, so paying for a Clipper2 pass
    /// on every <c>AOEShapeCustom</c> ever constructed would buy nothing for the modules that never ask.
    /// The ones that do ask are those that manipulate the polygon themselves — carving a hole per cell,
    /// growing a safe region — and for them it is built once, here.</para>
    ///
    /// <para><paramref name="skipPolygonInit"/> on the constructor exists for source compatibility and is
    /// not needed: being lazy is the same thing, done unconditionally.</para>
    /// </summary>
    public RelSimplifiedComplexPolygon Polygon
    {
        get
        {
            if (this.polygon == null)
            {
                this.polygonOrigin = this.origin;
                this.polygon = PolygonClipper.GetCombinedPolygon(this.origin, this.shapes1, this.difference, this.shapes2, this.operand);
                this.polygon.InitPolygonIndex();
            }
            return this.polygon;
        }
        set
        {
            this.polygon = value;
            this.polygonReplaced = true;
        }
    }

    /// <summary>
    /// Swap in a polygon computed by the caller, relative to <paramref name="newOrigin"/>.
    ///
    /// <para>From here on <see cref="Check"/> answers from this polygon rather than from the source
    /// shapes, because the caller has changed the shape and the originals no longer describe it. That
    /// switch is the point of the call, and it is why replacement is tracked separately from the lazy
    /// build above — an untouched shape keeps Minerva's direct evaluation.</para>
    /// </summary>
    public void ReplacePolygon(RelSimplifiedComplexPolygon poly, WPos newOrigin)
    {
        this.polygon = poly;
        this.polygon.InitPolygonIndex();
        this.polygonOrigin = newOrigin;
        this.polygonReplaced = true;
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
        // a caller-supplied polygon replaces the source shapes as the definition of this AOE
        if (this.polygonReplaced && this.polygon != null)
        {
            var hit = this.polygon.Contains(position - this.polygonOrigin);
            return this.InvertForbiddenZone ? !hit : hit;
        }

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

        return this.InvertForbiddenZone ? !inside : inside;
    }

    public override IReadOnlyList<WPos> Contour(WPos origin, Angle rotation)
        => this.shapes1.Count > 0 ? this.shapes1[0].ContourWorld() : [];

    /// <summary>
    /// One loop per union operand. <see cref="Check"/> has always evaluated all of them, so the module knew
    /// the right answer while the drawing showed a single operand — on Treno's line-of-sight cast that meant
    /// one of thirteen boulder shadows was painted as the safe ground.
    /// </summary>
    public override IReadOnlyList<IReadOnlyList<WPos>> Contours(WPos origin, Angle rotation)
    {
        var n = this.shapes1.Count;
        if (n == 0)
            return [];
        var loops = new IReadOnlyList<WPos>[n];
        for (var i = 0; i < n; ++i)
            loops[i] = this.shapes1[i].ContourWorld();
        return loops;
    }

    /// <summary>
    /// Analytic field rather than the ±1 boolean fallback, so the dodge can see which way the nearest safe
    /// ground lies. Only the union/difference form is exact here; an intersection or xor still falls back.
    /// </summary>
    public override ShapeDistance Distance(WPos origin, Angle rotation)
        => this.operand == OperandType.Union && this.shapes2.Count == 0
            ? new SDShapeSet(this.shapes1, this.difference, this.InvertForbiddenZone)
            : base.Distance(origin, rotation);

    public override string ToString() => $"CustomAOE u={this.shapes1.Count} d={this.difference.Count}";
}
