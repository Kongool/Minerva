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

/// <summary>
/// Forbids everything except one pathfinding cell at <paramref name="destination"/> — how a module says
/// "stand exactly here" rather than "stay out of there". Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
///
/// <para>The allowed box is sized by the pathfinder's own <paramref name="mapResolution"/>, so the target is
/// a whole cell rather than an infinitely thin point the search can never land on exactly.</para>
///
/// <para>Once the player is within <paramref name="tolerance"/> of the destination it forbids nothing at
/// all. Without that the zone keeps pulling at a player already standing correctly, and the result is a
/// character that jitters in place on the spot it was told to hold.</para>
/// </summary>
public sealed class SDPrecisePosition(WPos destination, WDir axis, float mapResolution, WPos currentPosition, float tolerance) : ShapeDistance
{
    private readonly bool satisfied = (currentPosition - destination).LengthSq() <= tolerance * tolerance;
    private readonly SDInvertedRect allowed = new(
        destination,
        axis.LengthSq() > 0f ? axis.Normalized() : new WDir(0f, 1f),
        mapResolution * 0.5f,
        mapResolution * 0.5f,
        mapResolution * 0.5f);

    public override float Distance(WPos p) => this.satisfied ? 1f : this.allowed.Distance(p);
}

/// <summary>Forbids everything outside the union — you must stand in one of the shapes. Two towers, three
/// safe puddles: the module names the safe spots and everything else becomes forbidden.</summary>
public sealed class SDInvertedUnion(ShapeDistance[] shapes) : ShapeDistance
{
    private readonly SDUnion inner = new(shapes);

    public override float Distance(WPos p) => -this.inner.Distance(p);
}

/// <summary>
/// A capsule swept along an arc: every point within <paramref name="tubeRadius"/> of the path a circle
/// traces orbiting <paramref name="orbitCenter"/> from <paramref name="start"/> through
/// <paramref name="angularLength"/>. This is what an orbiting AOE actually covers — a ball rolling round
/// the arena rather than a static wedge — and the two are quite different near the ends.
///
/// <para>Written from the geometry rather than ported from BossmodReborn's hand-optimised version: inside
/// the swept wedge the nearest centreline point is at the same angle, so the distance is <c>|d - R|</c>;
/// outside it, the nearest point is whichever end cap is closer. Both cases are exact.</para>
///
/// <para><paramref name="angularLength"/> is signed — negative sweeps the other way round — because a fight
/// with two counter-rotating orbits describes them that way.</para>
/// </summary>
public sealed class SDArcCapsule : ShapeDistance
{
    private readonly WPos orbitCenter;
    private readonly WPos startCap, endCap;
    private readonly float orbitRadius, tube;
    private readonly Angle from, to;

    public SDArcCapsule(WPos start, WPos orbitCenter, Angle angularLength, float tubeRadius)
    {
        this.orbitCenter = orbitCenter;
        this.tube = tubeRadius;

        var r0 = start - orbitCenter;
        this.orbitRadius = r0.Length();
        var startAngle = Angle.FromDirection(r0);
        var endAngle = startAngle + angularLength;

        // normalize so `from` is the lower bound of the swept range regardless of sweep direction
        (this.from, this.to) = angularLength.Rad >= 0f ? (startAngle, endAngle) : (endAngle, startAngle);

        this.startCap = start;
        this.endCap = orbitCenter + (r0.Rotate(angularLength));
    }

    public SDArcCapsule(WPos start, WDir toOrbitCenter, Angle angularLength, float tubeRadius)
        : this(start, start + toOrbitCenter, angularLength, tubeRadius)
    {
    }

    public override float Distance(WPos p)
    {
        var v = p - this.orbitCenter;
        var d = v.Length();

        // is this point's bearing inside the swept range? compare against the range midpoint so the test
        // works across the +/-pi seam, which a plain `from <= a && a <= to` does not
        var mid = (this.from + this.to) * 0.5f;
        var halfSpan = (this.to - this.from).Rad * 0.5f;
        var offset = Angle.FromDirection(v).DistanceToAngle(mid).Rad;

        var toCenterline = MathF.Abs(offset) <= halfSpan
            ? MathF.Abs(d - this.orbitRadius)
            : MathF.Min((p - this.startCap).Length(), (p - this.endCap).Length());

        return toCenterline - this.tube;
    }
}

/// <summary>Two perpendicular bars crossing at <paramref name="origin"/> — the plus/cross AOE.</summary>
public sealed class SDCross(WPos origin, Angle rotation, float length, float halfWidth) : ShapeDistance
{
    private readonly SDRect arm1 = new(origin, rotation, length, length, halfWidth);
    private readonly SDRect arm2 = new(origin, rotation + 90f.Degrees(), length, length, halfWidth);

    public override float Distance(WPos p) => MathF.Min(this.arm1.Distance(p), this.arm2.Distance(p));
}

/// <summary>Forbids everything outside the cross — the safe ground is the cross itself.</summary>
public sealed class SDInvertedCross(WPos origin, Angle rotation, float length, float halfWidth) : ShapeDistance
{
    private readonly SDCross inner = new(origin, rotation, length, halfWidth);

    public override float Distance(WPos p) => -this.inner.Distance(p);
}

/// <summary>Forbids everything outside the arc capsule — you must ride along with it.</summary>
public sealed class SDInvertedArcCapsule : ShapeDistance
{
    private readonly SDArcCapsule inner;

    public SDInvertedArcCapsule(WPos start, WPos orbitCenter, Angle angularLength, float tubeRadius)
        => this.inner = new SDArcCapsule(start, orbitCenter, angularLength, tubeRadius);

    public SDInvertedArcCapsule(WPos start, WDir toOrbitCenter, Angle angularLength, float tubeRadius)
        => this.inner = new SDArcCapsule(start, toOrbitCenter, angularLength, tubeRadius);

    public override float Distance(WPos p) => -this.inner.Distance(p);
}

/// <summary>Forbids everything outside the capsule — you must stay within it.</summary>
public sealed class SDInvertedCapsule(WPos origin, WDir direction, float length, float radius) : ShapeDistance
{
    private readonly SDCapsule inner = new(origin, direction, length, radius);

    public override float Distance(WPos p) => -this.inner.Distance(p);
}

/// <summary>
/// Forbids everything on the <paramref name="normal"/> side of the line through <paramref name="point"/>.
/// A whole half of the arena in one primitive — how a module says "get behind this line" for a wall of
/// AOEs or a knockback edge, without enumerating the shapes that make it up.
/// </summary>
public sealed class SDHalfPlane(WPos point, WDir normal) : ShapeDistance
{
    private readonly WDir unit = normal.LengthSq() > 0f ? normal.Normalized() : new WDir(0f, 1f);

    public override float Distance(WPos p) => -(p - point).Dot(this.unit);
}

/// <summary>Forbids everything except the donut sector — you must stand inside the wedge.</summary>
public sealed class SDInvertedDonutSector(WPos center, float innerRadius, float outerRadius, Angle direction, Angle halfAngle) : ShapeDistance
{
    private readonly SDDonutSector inner = new(center, innerRadius, outerRadius, direction, halfAngle);

    public override float Distance(WPos p) => -this.inner.Distance(p);
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

// Knockback-resolution SDFs now live in Automation/Knockback/, ported from BossmodReborn with their real
// implementations. They used to be stubs here that returned "nothing forbidden" for every point, which kept
// ported modules compiling but meant the auto-dodge was told a knockback could never put you anywhere fatal
// -- a mechanic that kills by throwing you off the edge simply did not exist as far as it was concerned.

/// <summary>
/// Forbids standing anywhere a fixed-direction knockback would push you out of an axis-aligned box.
///
/// <para>Boolean rather than graded, as BossmodReborn has it: the question "does this knock me off" has no
/// meaningful distance, since being one yard from the edge after the push is as alive as being ten. The
/// direction carries the distance and is not normalised — the caster scales it.</para>
/// </summary>
public sealed class SDKnockbackInAABBRectFixedDirection(WPos center, WDir direction, float halfWidth, float halfHeight) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        return (p + direction).InRect(center, halfWidth, halfHeight) ? 1f : -1f;
    }
}

/// <summary>Same, for a square arena that may be rotated off the axes.</summary>
public sealed class SDKnockbackInSquareFixedDirection(WPos center, WDir direction, float halfWidth, Angle rotation) : ShapeDistance
{
    private readonly WDir axis = rotation.ToDirection();

    public override float Distance(WPos p) => (p + direction).InSquare(center, halfWidth, this.axis) ? 1f : -1f;
}

/// <summary>
/// The AABB knockback check, plus voidzones you must also not land in. One shape rather than a union
/// because the question is about a single landing point: knocked out of the box or into a puddle are the
/// same answer, and only <paramref name="length"/> entries of <paramref name="origins"/> are live — the
/// caller reuses one buffer across frames.
/// </summary>
public sealed class SDKnockbackInAABBRectFixedDirectionPlusAOECircles(
    WPos center, WDir direction, float halfWidth, float halfHeight, WPos[] origins, float radius, int length) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var landing = p + direction;
        if (!landing.InRect(center, halfWidth, halfHeight))
            return -1f;

        for (var i = 0; i < length; ++i)
            if ((landing - origins[i]).LengthSq() <= radius * radius)
                return -1f;

        return 1f;
    }
}

/// <summary>
/// Forbids standing where a fixed-direction knockback would NOT be stopped by a wall.
///
/// <para>The inverse of the arena-boundary shapes: here the walls are what saves you, so the safe ground is
/// wherever the push runs into one within its own distance. Only <paramref name="length"/> entries of
/// <paramref name="safeWalls"/> are live — the caller reuses one buffer across frames.</para>
/// </summary>
public sealed class SDKnockbackFixedDirectionAgainstSafewalls(
    WDir direction, Components.GenericKnockback.SafeWall[] safeWalls, float distance, int length) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        for (var i = 0; i < length; ++i)
        {
            var w = safeWalls[i];
            if (Intersect.RaySegment(p, direction, w.Vertex1, w.Vertex2) < distance)
                return 1f;
        }

        return -1f;
    }
}

/// <summary>
/// Distance to an arbitrary polygon with holes — the shape a clipped arena or a carved-out safe region
/// actually is, once boolean geometry has had its way with it. Ported from BossmodReborn (BSD-3).
///
/// <para><b>The magnitude is only meaningful on one side.</b> Inside the polygon this answers 0, not a
/// signed depth, because the edge lookup is a coarse spatial grid: a point deep inside lands in a cell
/// holding no edges at all, and measuring from there would report infinity rather than a large number.
/// Answering the containment test first and only then measuring is what keeps that honest. Sign is still
/// correct for <see cref="ShapeDistance.Contains"/>, which is what forbidden zones read; treat the number
/// as a clearance only where it is positive.</para>
/// </summary>
public readonly struct SDPolygonWithHolesBase
{
    private readonly RelSimplifiedComplexPolygon polygon;
    private readonly float originX, originZ;
    private readonly Edge[] edges;
    private readonly SpatialIndex index;

    public SDPolygonWithHolesBase(WPos origin, RelSimplifiedComplexPolygon polygon)
    {
        this.originX = origin.X;
        this.originZ = origin.Z;
        this.polygon = polygon;

        var parts = polygon.Parts;
        var vertsCount = 0;
        for (var i = 0; i < parts.Count; ++i)
            vertsCount += parts[i].Vertices.Count;

        this.edges = new Edge[vertsCount];
        var at = 0;
        for (var i = 0; i < parts.Count; ++i)
        {
            var part = parts[i];
            at = AppendEdges(this.edges, at, part.Exterior, origin);
            for (var j = 0; j < part.HoleStarts.Count; ++j)
                at = AppendEdges(this.edges, at, part.Interior(j), origin);
        }

        this.index = new SpatialIndex(this.edges);

        static int AppendEdges(Edge[] dst, int index, ReadOnlySpan<WDir> vertices, WPos origin)
        {
            if (vertices.Length == 0)
                return index;

            var prev = vertices[^1];
            for (var i = 0; i < vertices.Length; ++i)
            {
                var curr = vertices[i];
                dst[index++] = new Edge(origin.X + prev.X, origin.Z + prev.Z, curr.X - prev.X, curr.Z - prev.Z);
                prev = curr;
            }

            return index;
        }
    }

    public readonly bool Contains(WPos p) => this.polygon.Contains(new WDir(p.X - this.originX, p.Z - this.originZ));

    /// <summary>Distance to the boundary from outside; 0 anywhere inside.</summary>
    public readonly float Distance(WPos p) => this.Contains(p) ? 0f : this.EdgeDistance(p);

    /// <summary>Distance to the boundary from inside; 0 anywhere outside. The inverse case.</summary>
    public readonly float DistanceInverted(WPos p) => this.Contains(p) ? this.EdgeDistance(p) : 0f;

    private readonly float EdgeDistance(WPos p)
    {
        var best = float.MaxValue;
        var near = this.index.Query(p.X, p.Z);
        for (var i = 0; i < near.Length; ++i)
        {
            ref readonly var e = ref this.edges[near[i]];
            var t = Math.Clamp((((p.X - e.Ax) * e.Dx) + ((p.Z - e.Ay) * e.Dy)) * e.InvLengthSq, 0f, 1f);
            var dx = p.X - (e.Ax + (t * e.Dx));
            var dz = p.Z - (e.Ay + (t * e.Dy));
            best = Math.Min(best, (dx * dx) + (dz * dz));
        }

        // no edges in this cell -- too far from the boundary for the grid to say. Report "far", never
        // sqrt(float.MaxValue), which would read as a real clearance to anything comparing against it.
        return best == float.MaxValue ? 1000f : MathF.Sqrt(best);
    }
}

/// <summary>Forbids the inside of a polygon with holes.</summary>
public sealed class SDPolygonWithHoles(SDPolygonWithHolesBase core) : ShapeDistance
{
    public override float Distance(WPos p) => core.Contains(p) ? -1f : core.Distance(p);
}

/// <summary>Forbids everything outside a polygon with holes — you must stay inside it.</summary>
public sealed class SDInvertedPolygonWithHoles(SDPolygonWithHolesBase core) : ShapeDistance
{
    public override float Distance(WPos p) => core.Contains(p) ? core.DistanceInverted(p) : -1f;
}

/// <summary>Forbids everything outside a complex polygon, tested by containment only.</summary>
public sealed class SDComplexPolygonInvertedContains(RelSimplifiedComplexPolygon polygon, WPos center) : ShapeDistance
{
    public override float Distance(WPos p) => polygon.Contains(p - center) ? 1f : -1f;
}

// More of BossmodReborn's knockback-survival shapes. All boolean: "does this shove put me somewhere fatal"
// has no useful magnitude, since one yard past the edge is as dead as ten. BMR spells that as
// `Contains ? 0f : 1f`; Minerva's convention is negative-is-forbidden, so these answer -1f / +1f. Copying
// BMR's 0f verbatim would make every point read as contained and forbid the entire arena, silently.

/// <summary>Forbids ground from which a knockback away from an origin would leave an axis-aligned box.</summary>
public sealed class SDKnockbackInAABBRectAwayFromOrigin(WPos center, WPos origin, float distance, float halfWidth, float halfHeight) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        return (p + (distance * (p - origin).Normalized())).InRect(center, halfWidth, halfHeight) ? 1f : -1f;
    }
}

/// <summary>
/// A knockback that pushes north or south depending on which side of the arena centre you stand — the
/// "blown to your own half" shape. Forbids ground from which that push leaves the box.
/// </summary>
public sealed class SDKnockbackInAABBRectLeftRightAlongZAxis(WPos center, float distance, float halfWidth, float halfHeight) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        return (p + new WDir(0f, p.Z > center.Z ? distance : -distance)).InRect(center, halfWidth, halfHeight) ? 1f : -1f;
    }
}

/// <summary>The same, with rectangular AOEs you must also not be pushed into.</summary>
public sealed class SDKnockbackInAABBRectLeftRightAlongZAxisPlusAOERects(
    WPos center, float distance, float halfWidth, float halfHeight,
    (WPos Origin, WDir Direction)[] aoes, float lengthFront, float rectHalfWidth, int length) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var landing = p + new WDir(0f, p.Z > center.Z ? distance : -distance);
        if (!landing.InRect(center, halfWidth, halfHeight))
            return -1f;

        for (var i = 0; i < length; ++i)
            if (landing.InRect(aoes[i].Origin, aoes[i].Direction, lengthFront, 0f, rectHalfWidth))
                return -1f;

        return 1f;
    }
}

/// <summary>
/// A fixed-direction knockback stopped by walls, where the spot you are stopped at must also not be inside
/// a rectangular AOE. Being saved by a wall is no good if the wall is where the cleave lands.
/// </summary>
public sealed class SDKnockbackFixedDirectionAgainstSafewallsPlusRectAOE(
    WDir direction, Components.GenericKnockback.SafeWall[] safeWalls, float distance, int length,
    WPos rectOrigin, WDir rectDirection, float lengthFront, float halfWidth) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        for (var i = 0; i < length; ++i)
        {
            var w = safeWalls[i];
            var hit = Intersect.RaySegment(p, direction, w.Vertex1, w.Vertex2);
            if (hit < distance)
            {
                // stopped by this wall -- safe only if the stopping point is clear of the AOE
                return (p + (hit * direction)).InRect(rectOrigin, rectDirection, lengthFront, 0f, halfWidth) ? -1f : 1f;
            }
        }

        return -1f; // no wall stops the push
    }
}

/// <summary>
/// Distance to a triangle, as the largest of its three edge half-plane distances.
///
/// <para>Unlike the boolean knockback shapes above this is a real signed distance — negative inside,
/// positive outside, in yalms — because a triangle is convex and the max-of-half-planes formulation is
/// exact for convex shapes. The winding is normalised in the constructor so a triangle given clockwise and
/// the same one given anticlockwise produce identical answers rather than inverted ones.</para>
/// </summary>
public sealed class SDTri : ShapeDistance
{
    private readonly WDir n1, n2, n3;
    private readonly WPos a, b, c;

    public SDTri(WPos origin, RelTriangle triangle)
    {
        var ab = triangle.B - triangle.A;
        var bc = triangle.C - triangle.B;
        var ca = triangle.A - triangle.C;
        this.n1 = ab.OrthoL().Normalized();
        this.n2 = bc.OrthoL().Normalized();
        this.n3 = ca.OrthoL().Normalized();
        if (ab.Cross(bc) < 0f)
        {
            this.n1 = -this.n1;
            this.n2 = -this.n2;
            this.n3 = -this.n3;
        }

        this.a = origin + triangle.A;
        this.b = origin + triangle.B;
        this.c = origin + triangle.C;
    }

    public override float Distance(WPos p)
    {
        var d1 = this.n1.Dot(p - this.a);
        var d2 = this.n2.Dot(p - this.b);
        var d3 = this.n3.Dot(p - this.c);
        return Math.Max(Math.Max(d1, d2), d3);
    }
}

/// <summary>Forbids everything outside a triangle — you must stand inside it.</summary>
public sealed class SDInvertedTri : ShapeDistance
{
    private readonly SDTri inner;

    public SDInvertedTri(WPos origin, RelTriangle triangle) => this.inner = new SDTri(origin, triangle);

    public override float Distance(WPos p) => -this.inner.Distance(p);
}

/// <summary>
/// A pull toward an origin, where the path must clear both voidzone circles and a square tile.
///
/// <para>Note it tests the whole SWEPT path against the tile, not just the landing point: an attract drags
/// you through everything between here and there, so a tile you merely cross still kills. The circles are
/// tested at the landing point because those are what you end up standing in.</para>
/// </summary>
public sealed class SDKnockbackTowardsOriginPlusAOECirclesPlusAABBSquareIntersection(
    WPos center, float distance, WPos[] aoes, float radius, WPos centerTile, float tileHalfWidth, int length) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var offset = p - center;
        var len = offset.Length();
        var travel = len > distance ? distance : len;
        var dir = len > 0f ? -offset / len : default;
        var landing = p + (travel * dir);

        for (var i = 0; i < length; ++i)
            if ((landing - aoes[i]).LengthSq() <= radius * radius)
                return -1f;

        return Intersect.RayAABB(centerTile - center, dir, tileHalfWidth, tileHalfWidth) <= travel ? -1f : 1f;
    }
}

/// <summary>
/// Knockbacks aimed out of rectangles, where the only safe landing is inside a donut's hole.
///
/// <para><b>Inverted: almost everything is forbidden.</b> The only safe ground is inside a knockback
/// rectangle that happens to throw you into a hole. Standing where no knockback reaches is forbidden too —
/// this shape is used where the mechanic is survived by BEING pushed, so not being pushed is the failure.
/// Reading it the intuitive way round ("outside the rectangles is fine") inverts the whole zone.</para>
/// </summary>
public sealed class SDKnockbackWithWallsAwayFromOriginMultiAimIntoDonuts(
    (WPos Origin, WDir Direction)[] knockbacks, int lengthKnockbacks, float rectLengthFront, float rectHalfWidth,
    float distance, WPos[] donutOrigins, float donutInnerRadius, int lengthDonuts) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        for (var i = 0; i < lengthKnockbacks; ++i)
        {
            var kb = knockbacks[i];
            if (!p.InRect(kb.Origin, kb.Direction, rectLengthFront, 0f, rectHalfWidth))
                continue;

            var landing = p + (distance * (kb.Origin - p).Normalized());
            for (var j = 0; j < lengthDonuts; ++j)
                if ((landing - donutOrigins[j]).LengthSq() <= donutInnerRadius * donutInnerRadius)
                    return 1f; // thrown into a hole -- the safe outcome

            return -1f;
        }

        return -1f; // no knockback reaches here, so nothing saves you either
    }
}

/// <summary>
/// Signed distance to a convex polygon, given its vertices in order.
///
/// <para>A true distance in yalms, not a boolean: for a convex shape the nearest edge's perpendicular
/// distance IS the distance, so the sign comes free from which side of every edge the point falls on.</para>
///
/// <para><paramref name="cw"/> states the winding. It cannot be inferred per-edge and getting it wrong
/// inverts the shape exactly — every safe cell becomes forbidden and the arena reads as solid.</para>
/// </summary>
public sealed class SDConvexPolygon : ShapeDistance
{
    private readonly (WPos A, WPos B)[] edges;
    private readonly bool cw;

    public SDConvexPolygon((WPos, WPos)[] edges, bool cw)
    {
        this.edges = edges;
        this.cw = cw;
    }

    public SDConvexPolygon(ReadOnlySpan<WPos> vertices, bool cw)
    {
        this.edges = new (WPos, WPos)[vertices.Length];
        for (var i = 0; i < vertices.Length; ++i)
            this.edges[i] = (vertices[i], vertices[(i + 1) % vertices.Length]);
        this.cw = cw;
    }

    public override float Distance(WPos p)
    {
        var best = float.MaxValue;
        var inside = true;
        for (var i = 0; i < this.edges.Length; ++i)
        {
            var (a, b) = this.edges[i];
            var ab = b - a;
            var ap = p - a;
            var d = ((ab.X * ap.Z) - (ab.Z * ap.X)) / ab.Length();
            if ((this.cw && d > 0f) || (!this.cw && d < 0f))
                inside = false;
            best = Math.Min(best, Math.Abs(d));
        }

        return inside ? -best : best;
    }
}

/// <summary>
/// The floor of T01 Caduceus: thirteen platforms at different heights, with edges you may only cross from
/// below.
///
/// <para>Everything off a platform is forbidden — that is the inverted union in the middle. Then each high
/// edge is added back as forbidden ONLY while the player is below the platform it leads to, which is why
/// the shape needs the actor's Y: the same edge is a wall from underneath and a doorway from on top, and a
/// purely 2D arena cannot tell those apart.</para>
/// </summary>
public sealed class SDBlockedAreaT01Caduceus(
    ShapeDistance[] platformShapes, (int Lower, int Upper)[] highEdges, ShapeDistance[] highEdgeShapes,
    float actorY, float[] platformHeights) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var res = float.MaxValue;
        for (var i = 0; i < platformShapes.Length; ++i)
            res = Math.Min(res, platformShapes[i].Distance(p));

        res = -res; // off a platform is forbidden, so the union inverts

        for (var i = 0; i < highEdges.Length && i < highEdgeShapes.Length; ++i)
        {
            var e = highEdges[i];
            if (actorY + 0.1f < platformHeights[e.Upper])
                res = Math.Min(res, highEdgeShapes[i].Distance(p));
        }

        return res;
    }
}

/// <summary>
/// An inverted union — you must be inside one of the zones — with a constant added to the result.
///
/// <para>The offset shifts the safe boundary without moving the shapes: positive pulls it inward, so the
/// dodge stops short of the true edge. A module uses it to keep clearance from a boundary the shapes
/// already describe exactly, rather than rebuilding every zone a yard smaller.</para>
/// </summary>
public sealed class SDInvertedUnionOffset(ShapeDistance[] zones, float offset) : ShapeDistance
{
    public override float Distance(WPos p)
    {
        var min = float.MaxValue;
        for (var i = 0; i < zones.Length; ++i)
            min = Math.Min(min, zones[i].Distance(p));
        return -min + offset;
    }
}
