namespace Minerva;

/// <summary>
/// Approximating curves with line segments. Clipping and rendering both work in polygons, so every circle
/// and arc has to become one somewhere; this is where. Ported from BossmodReborn (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
///
/// <para>Angles use the game's convention throughout — 0 is south, increasing clockwise — matching
/// <see cref="Angle"/> elsewhere in Minerva, so a direction taken from an actor's rotation can be handed
/// straight in.</para>
/// </summary>
public static class CurveApprox
{
    /// <summary>Tessellation error that reads as smooth at radar scale, in yalms.</summary>
    public const float ScreenError = 0.05f;

    /// <summary>
    /// How many segments a curve needs so it never deviates from the true arc by more than
    /// <paramref name="maxError"/>.
    ///
    /// <para>From the sagitta: <c>error = R(1 - cos(φ/2))</c>, solved for φ. Rounded UP to an even count so
    /// a full circle stays symmetric about both axes — an odd count puts a vertex on one side and an edge
    /// on the other, which shows up as a lopsided outline. Clamped to [4, 512]: below four it is not a
    /// curve, above 512 the extra vertices cost more than they show.</para>
    /// </summary>
    public static int CalculateCircleSegments(float radius, Angle angularLength, float maxError)
    {
        var tessAngle = 2f * MathF.Acos(1f - Math.Min(maxError / radius, 1f));
        var segments = (int)MathF.Ceiling(angularLength.Rad / tessAngle);
        segments = (segments + 1) & ~1;
        return Math.Clamp(segments, 4, 512);
    }

    /// <summary>A full circle's points, anticlockwise. Implicitly closed — the last point is not repeated.</summary>
    public static WDir[] Circle(float radius, float maxError)
    {
        var segments = CalculateCircleSegments(radius, Angle.DoublePI.Radians(), maxError);
        var step = (Angle.DoublePI / segments).Radians();
        var points = new WDir[segments];
        for (var i = 0; i < segments; ++i)
            points[i] = radius * (i * step).ToDirection();
        return points;
    }

    /// <summary>The same, offset from the origin.</summary>
    public static WDir[] Circle(WDir centerOffset, float radius, float maxError)
    {
        var points = Circle(radius, maxError);
        for (var i = 0; i < points.Length; ++i)
            points[i] += centerOffset;
        return points;
    }

    /// <summary>An open arc from <paramref name="angleStart"/> to <paramref name="angleEnd"/>, both ends
    /// included — unlike <see cref="Circle(float, float)"/>, which closes implicitly.</summary>
    public static WDir[] CircleArc(float radius, Angle angleStart, Angle angleEnd, float maxError)
    {
        var length = angleEnd - angleStart;
        var segments = CalculateCircleSegments(radius, length.Abs(), maxError);
        var step = length / segments;
        var points = new WDir[segments + 1];
        for (var i = 0; i <= segments; ++i)
            points[i] = radius * (angleStart + (i * step)).ToDirection();
        return points;
    }

    /// <summary>A pie slice: the centre, then the arc closing back to it.</summary>
    public static WDir[] CircleSector(WDir centerOffset, float radius, Angle angleStart, Angle angleEnd, float maxError)
    {
        var arc = CircleArc(radius, angleStart, angleEnd, maxError);
        var points = new WDir[arc.Length + 1];
        points[0] = centerOffset;
        for (var i = 0; i < arc.Length; ++i)
            points[i + 1] = arc[i] + centerOffset;
        return points;
    }

    /// <summary>An annular sector — the outer arc out, the inner arc back.</summary>
    public static WDir[] DonutSector(WDir centerOffset, float innerRadius, float outerRadius, Angle angleStart, Angle angleEnd, float maxError)
    {
        var outer = CircleArc(outerRadius, angleStart, angleEnd, maxError);
        var inner = CircleArc(innerRadius, angleEnd, angleStart, maxError);
        var points = new WDir[outer.Length + inner.Length];
        for (var i = 0; i < outer.Length; ++i)
            points[i] = outer[i] + centerOffset;
        for (var i = 0; i < inner.Length; ++i)
            points[outer.Length + i] = inner[i] + centerOffset;
        return points;
    }
}
