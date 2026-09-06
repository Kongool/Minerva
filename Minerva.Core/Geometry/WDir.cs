namespace Minerva;

/// <summary>2D world-space direction on the XZ plane (Y is vertical and ignored here).</summary>
public readonly struct WDir(float x, float z)
{
    public readonly float X = x;
    public readonly float Z = z;

    public WDir(Vector2 v) : this(v.X, v.Y) { }

    public Vector2 ToVec2() => new(X, Z);
    public Vector3 ToVec3(float y = 0f) => new(X, y, Z);

    public static bool operator ==(WDir a, WDir b) => a.X == b.X && a.Z == b.Z;
    public static bool operator !=(WDir a, WDir b) => a.X != b.X || a.Z != b.Z;
    public static WDir operator +(WDir a, WDir b) => new(a.X + b.X, a.Z + b.Z);
    public static WDir operator -(WDir a, WDir b) => new(a.X - b.X, a.Z - b.Z);
    public static WDir operator -(WDir a) => new(-a.X, -a.Z);
    public static WDir operator *(WDir a, float b) => new(a.X * b, a.Z * b);
    public static WDir operator *(float a, WDir b) => new(a * b.X, a * b.Z);
    public static WDir operator /(WDir a, float b) => new(a.X / b, a.Z / b);

    public WDir Abs() => new(MathF.Abs(X), MathF.Abs(Z));
    public WDir OrthoL() => new(Z, -X); // 90° CCW, same length

    /// <summary>Rotate by an arbitrary angle, same length. OrthoL/OrthoR are the 90° special cases.</summary>
    public WDir Rotate(Angle a)
    {
        var (s, c) = (MathF.Sin(a.Rad), MathF.Cos(a.Rad));
        return new((this.X * c) + (this.Z * s), (this.Z * c) - (this.X * s));
    }
    public WDir OrthoR() => new(-Z, X); // 90° CW, same length

    /// <summary>Mirrored across the Z axis (X negated) — the same direction on the other side of a
    /// north-south line, which is how a fight's east half maps onto its west.</summary>
    public WDir MirrorX() => new(-this.X, this.Z);

    /// <summary>Mirrored across the X axis (Z negated).</summary>
    public WDir MirrorZ() => new(this.X, -this.Z);

    public static float Dot(WDir a, WDir b) => a.X * b.X + a.Z * b.Z;
    public float Dot(WDir a) => X * a.X + Z * a.Z;
    public float Cross(WDir b) => X * b.Z - Z * b.X;

    public float LengthSq() => X * X + Z * Z;
    public float Length() => MathF.Sqrt(LengthSq());

    public WDir Normalized()
    {
        var len = Length();
        return len > 0f ? this / len : default;
    }

    public Angle ToAngle() => new(MathF.Atan2(X, Z));
    public bool AlmostEqual(WDir b, float eps) => MathF.Abs(X - b.X) <= eps && MathF.Abs(Z - b.Z) <= eps;

    // area checks, treating 'this' as an offset from a shape's center
    /// <summary>Rotate by the angle <paramref name="dir"/> points at, keeping this vector's length.</summary>
    public WDir Rotate(WDir dir) => this.Rotate(dir.ToAngle());

    public readonly WDir Scaled(float multiplier) => new(this.X * multiplier, this.Z * multiplier);

    /// <summary>Snapped to whole yalms. Fights that lay out on a grid compare positions this way, so a
    /// fraction of a yalm of drift does not make two tiles look different.</summary>
    public readonly WDir Rounded() => new(MathF.Round(this.X), MathF.Round(this.Z));

    /// <inheritdoc cref="Rounded()"/>
    public readonly WDir Rounded(float precision) => this.Scaled(1f / precision).Rounded().Scaled(precision);

    public bool InRect(WDir direction, float lenFront, float lenBack, float halfWidth)
    {
        var dotDir = Dot(direction);
        var dotNormal = Dot(direction.OrthoL());
        return dotDir >= -lenBack && dotDir <= lenFront && MathF.Abs(dotNormal) <= halfWidth;
    }

    public bool InCross(WDir direction, float length, float halfWidth)
    {
        var dotDir = Dot(direction);
        var absNormal = MathF.Abs(Dot(direction.OrthoL()));
        var inVertical = dotDir >= -length && dotDir <= length && absNormal <= halfWidth;
        var inHorizontal = dotDir >= -halfWidth && dotDir <= halfWidth && absNormal <= length;
        return inVertical || inHorizontal;
    }

    public override string ToString() => $"({X:f3}, {Z:f3})";
    public bool Equals(WDir other) => this == other;
    public override bool Equals(object? obj) => obj is WDir other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Z);

    /// <summary>Componentwise sign, so a direction collapses to one of the nine cardinal/diagonal unit
    /// steps. Modules use it to turn "roughly northeast" into the corner of a square arena.</summary>
    public readonly WDir Sign() => new(MathF.Sign(this.X), MathF.Sign(this.Z));
}
