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
    public WDir OrthoR() => new(-Z, X); // 90° CW, same length

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
}
