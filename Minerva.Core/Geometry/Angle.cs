namespace Minerva;

/// <summary>
/// Type-safe wrapper around a float angle stored in radians.
/// World rotation convention (matches the game): 0 = facing south / (0,-1), increasing
/// counter-clockwise, so +90° = facing east / (1,0).
/// </summary>
public readonly struct Angle(float rad)
{
    public readonly float Rad = rad;

    public const float RadToDeg = 180f / MathF.PI;
    public const float DegToRad = MathF.PI / 180f;
    public const float TwoPI = MathF.Tau;

    /// <summary>BossmodReborn's name for <see cref="TwoPI"/>.</summary>
    public const float DoublePI = MathF.Tau;
    public const float Pi = MathF.PI;
    public const float HalfPi = MathF.PI / 2f;

    // cardinal / intercardinal facings (matching BMR), for modules that key off boss orientation
    public static readonly Angle[] AnglesIntercardinals = [(-45.003f).Degrees(), 44.998f.Degrees(), 134.999f.Degrees(), (-135.005f).Degrees()];
    public static readonly Angle[] AnglesCardinals = [(-90.004f).Degrees(), (-0.003f).Degrees(), 180f.Degrees(), 89.999f.Degrees()];

    public float Deg => Rad * RadToDeg;

    public static Angle FromDirection(WDir dir) => new(MathF.Atan2(dir.X, dir.Z));

    /// <summary>Inverse sine, as an angle. Clamped, because a cosine rule fed near-degenerate geometry
    /// lands a hair outside [-1, 1] and <c>Math.Asin</c> answers NaN, which then poisons every angle it
    /// touches instead of failing where you can see it.</summary>
    public static Angle Asin(float x) => new(MathF.Asin(Math.Clamp(x, -1f, 1f)));

    /// <summary>Inverse cosine, as an angle. Clamped for the same reason as <see cref="Asin"/>.</summary>
    public static Angle Acos(float x) => new(MathF.Acos(Math.Clamp(x, -1f, 1f)));

    /// <summary>Angle of the direction (x, z), matching Minerva's convention where 0 points south.</summary>
    public static Angle Atan2(float x, float z) => new(MathF.Atan2(x, z));

    // Ordering comparisons on the raw radian value, so ported code can write `a <= b`. Note this compares
    // the stored value, not the shortest way round: normalize first if that is what you meant.
    public static bool operator <(Angle a, Angle b) => a.Rad < b.Rad;
    public static bool operator >(Angle a, Angle b) => a.Rad > b.Rad;
    public static bool operator <=(Angle a, Angle b) => a.Rad <= b.Rad;
    public static bool operator >=(Angle a, Angle b) => a.Rad >= b.Rad;

    public WDir ToDirection()
    {
        var (sin, cos) = MathF.SinCos(Rad);
        return new(sin, cos);
    }

    public static bool operator ==(Angle a, Angle b) => a.Rad == b.Rad;
    public static bool operator !=(Angle a, Angle b) => a.Rad != b.Rad;
    public static Angle operator +(Angle a, Angle b) => new(a.Rad + b.Rad);
    public static Angle operator -(Angle a, Angle b) => new(a.Rad - b.Rad);
    public static Angle operator -(Angle a) => new(-a.Rad);
    public static Angle operator *(Angle a, float b) => new(a.Rad * b);
    public static Angle operator *(float a, Angle b) => new(a * b.Rad);
    public static Angle operator /(Angle a, float b) => new(a.Rad / b);

    public Angle Abs() => new(MathF.Abs(Rad));
    public float Sin() => MathF.Sin(Rad);
    public float Cos() => MathF.Cos(Rad);
    public float Tan() => MathF.Tan(Rad);

    /// <summary>Wraps the angle into (-π, π].</summary>
    public Angle Normalized()
    {
        var r = Rad;
        while (r < -MathF.PI)
            r += TwoPI;
        while (r > MathF.PI)
            r -= TwoPI;
        return new(r);
    }

    public bool AlmostEqual(Angle other, float epsRad) => MathF.Abs((this - other).Normalized().Rad) <= epsRad;
    public Angle Round(float precisionDeg = 1f) => new(MathF.Round(Deg / precisionDeg) * precisionDeg * DegToRad);

    /// <summary>Shortest signed rotation from this angle to <paramref name="other"/> (&gt;0 = CCW).</summary>
    public Angle DistanceToAngle(Angle other) => (other - this).Normalized();

    /// <summary>
    /// How far this angle sits outside the range [<paramref name="min"/>, <paramref name="max"/>]: zero when
    /// inside, positive when <paramref name="min"/> is the nearer edge, negative when <paramref name="max"/>
    /// is. Modules use the sign to decide which way to rotate back into a safe wedge.
    /// </summary>
    /// <summary>The nearest angle inside [<paramref name="min"/>, <paramref name="max"/>] — this angle when
    /// already inside, otherwise whichever edge is closer. How a module snaps a facing into a safe wedge.</summary>
    public Angle ClosestInRange(Angle min, Angle max)
    {
        var width = (max - min) * 0.5f;
        var midDist = this.DistanceToAngle((min + max) * 0.5f);
        return midDist.Rad > width.Rad ? min : midDist.Rad < -width.Rad ? max : this;
    }

    public Angle DistanceToRange(Angle min, Angle max)
    {
        var width = (max - min) * 0.5f;
        var midDist = this.DistanceToAngle((min + max) * 0.5f);
        return midDist.Rad > width.Rad ? midDist - width
            : midDist.Rad < -width.Rad ? midDist + width
            : default;
    }

    public override string ToString() => Deg.ToString("f3", System.Globalization.CultureInfo.InvariantCulture);
    public bool Equals(Angle other) => Rad == other.Rad;
    public override bool Equals(object? obj) => obj is Angle other && Equals(other);
    public override int GetHashCode() => Rad.GetHashCode();
}

public static class AngleExtensions
{
    public static Angle Radians(this float radians) => new(radians);
    public static Angle Degrees(this float degrees) => new(degrees * Angle.DegToRad);
    public static Angle Degrees(this int degrees) => new(degrees * Angle.DegToRad);
}

/// <summary>
/// Radius scale factors, ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). A regular N-gon
/// drawn through a circle's radius sits <i>inside</i> the circle; multiplying the radius by
/// <c>1 / cos(pi / N)</c> pushes its vertices out far enough that the polygon circumscribes the circle
/// instead. Arena bounds built from polygons use these so the walkable area is not quietly clipped at every
/// edge midpoint.
/// </summary>
public static class CosPI
{
    public const float Pi8th = 1.082392f;    // 1 / cos(pi / 8)
    public const float Pi28th = 1.006328f;   // 1 / cos(pi / 28)
    public const float Pi32th = 1.004839f;   // 1 / cos(pi / 32)
    public const float Pi36th = 1.00382f;    // 1 / cos(pi / 36)
    public const float Pi40th = 1.0030922f;  // 1 / cos(pi / 40)
    public const float Pi48th = 1.0021457f;  // 1 / cos(pi / 48)
    public const float Pi60th = 1.0013723f;  // 1 / cos(pi / 60)
    public const float Pi64th = 1.001206f;   // 1 / cos(pi / 64)
    public const float Pi148th = 1.000225f;  // 1 / cos(pi / 148)
}
