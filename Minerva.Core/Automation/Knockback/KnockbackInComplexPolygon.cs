using System.Runtime.CompilerServices;

// Knockback-resolution shape distances, ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
//
// Each answers one question: "if the knockback fires while I stand here, where does it put me, and is that
// somewhere I survive?" They are boolean fields -- 0 where standing is fatal, 1 where it is not -- because
// the answer genuinely is binary: you are either thrown off the edge or you are not, and there is no
// gradient between them for a solver to descend.
//
// BossmodReborn's signatures use `in WPos` and override a virtual `Contains`/`RowIntersectsShape` that
// Minerva's ShapeDistance does not have, so those are converted here: Contains becomes a private helper and
// the pathfinding-grid fast path is dropped.

namespace Minerva;

[SkipLocalsInit]
public sealed class SDKnockbackInComplexPolygonAwayFromOrigin : ShapeDistance
{
    public SDKnockbackInComplexPolygonAwayFromOrigin(WPos Center, WPos Origin, float Distance, RelSimplifiedComplexPolygon Polygon)
    {
        center = Center;
        origin = Origin;
        distance = Distance;
        polygon = Polygon;
        polygon.VerifyPolygonIndexExistance();
    }

    private readonly WPos center;
    private readonly WPos origin;
    private readonly float distance;
    private readonly RelSimplifiedComplexPolygon polygon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Inside(WPos p) => !polygon.Contains(p - center + distance * (p - origin).Normalized());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float Distance(WPos p) => Inside(p) ? 0f : 1f;

}

[SkipLocalsInit]
public sealed class SDKnockbackInComplexPolygonFixedDirection : ShapeDistance
{
    public SDKnockbackInComplexPolygonFixedDirection(WPos Center, WDir Direction, RelSimplifiedComplexPolygon Polygon)
    {
        center = Center;
        direction = Direction;
        polygon = Polygon;
        polygon.VerifyPolygonIndexExistance();
    }

    private readonly WPos center;
    private readonly WDir direction;
    private readonly RelSimplifiedComplexPolygon polygon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Inside(WPos p) => !polygon.Contains(p - center + direction);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float Distance(WPos p) => Inside(p) ? 0f : 1f;

}

[SkipLocalsInit]
public sealed class SDKnockbackInComplexPolygonAwayFromOriginPlusAOEAABBSquares : ShapeDistance
{
    public SDKnockbackInComplexPolygonAwayFromOriginPlusAOEAABBSquares(WPos Center, WPos Origin, float Distance, RelSimplifiedComplexPolygon Polygon, WPos[] AOEs, float HalfWidth, int Length)
    {
        center = Center;
        origin = Origin;
        polygon = Polygon;
        distance = Distance;
        aoes = AOEs;
        halfWidth = HalfWidth;
        len = Length;
        polygon.VerifyPolygonIndexExistance();
    }

    private readonly WPos center;
    private readonly WPos origin;
    private readonly RelSimplifiedComplexPolygon polygon;
    private readonly float distance;
    private readonly WPos[] aoes;
    private readonly float halfWidth;
    private readonly int len;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float Distance(WPos p) => Inside(p) ? 0f : 1f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Inside(WPos p)
    {
        var dir = distance * (p - origin).Normalized();
        if (!polygon.Contains(p - center + dir))
        {
            return true;
        }

        var projected = p + dir;
        for (var i = 0; i < len; ++i)
        {
            if (projected.InSquare(aoes[i], halfWidth))
            {
                return true;
            }
        }
        return false;
    }

}

[SkipLocalsInit]
public sealed class SDKnockbackInComplexPolygonAwayFromOriginPlusAOECircles : ShapeDistance
{
    public SDKnockbackInComplexPolygonAwayFromOriginPlusAOECircles(WPos Center, WPos Origin, float Distance, RelSimplifiedComplexPolygon Polygon, WPos[] AOEs, float Radius, int Length)
    {
        center = Center;
        origin = Origin;
        polygon = Polygon;
        distance = Distance;
        aoes = AOEs;
        radius = Radius;
        len = Length;
        polygon.VerifyPolygonIndexExistance();
    }

    private readonly WPos center;
    private readonly WPos origin;
    private readonly RelSimplifiedComplexPolygon polygon;
    private readonly float distance;
    private readonly WPos[] aoes;
    private readonly float radius;
    private readonly int len;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Inside(WPos p)
    {
        var dir = distance * (p - origin).Normalized();
        if (!polygon.Contains(p - center + dir))
        {
            return true;
        }

        var projected = p + dir;
        for (var i = 0; i < len; ++i)
        {
            if (projected.InCircle(aoes[i], radius))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float Distance(WPos p) => Inside(p) ? 0f : 1f;

}

[SkipLocalsInit]
public sealed class SDKnockbackInComplexPolygonAwayFromOriginPlusIntersectionTest : ShapeDistance
{
    public SDKnockbackInComplexPolygonAwayFromOriginPlusIntersectionTest(WPos Center, WPos Origin, float Distance, RelSimplifiedComplexPolygon Polygon)
    {
        center = Center;
        origin = Origin;
        distance = Distance;
        polygon = Polygon;
        polygon.VerifyPolygonIndexExistance();
    }

    private readonly WPos center;
    private readonly WPos origin;
    private readonly float distance;
    private readonly RelSimplifiedComplexPolygon polygon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Inside(WPos p)
    {
        var offset = p - center;
        var dir = (p - origin).Normalized();
        return !polygon.Contains(offset + distance * dir) || polygon.Raycast(offset, dir) < distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float Distance(WPos p) => Inside(p) ? 0f : 1f;

}
