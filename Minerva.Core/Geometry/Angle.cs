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
    public const float Pi = MathF.PI;
    public const float HalfPi = MathF.PI / 2f;

    // cardinal / intercardinal facings (matching BMR), for modules that key off boss orientation
    public static readonly Angle[] AnglesIntercardinals = [(-45.003f).Degrees(), 44.998f.Degrees(), 134.999f.Degrees(), (-135.005f).Degrees()];
    public static readonly Angle[] AnglesCardinals = [(-90.004f).Degrees(), (-0.003f).Degrees(), 180f.Degrees(), 89.999f.Degrees()];

    public float Deg => Rad * RadToDeg;

    public static Angle FromDirection(WDir dir) => new(MathF.Atan2(dir.X, dir.Z));

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
