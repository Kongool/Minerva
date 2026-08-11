namespace Minerva;

/// <summary>2D world-space position on the XZ plane (Y is vertical and ignored here).</summary>
public readonly struct WPos(float x, float z)
{
    public readonly float X = x;
    public readonly float Z = z;

    public WPos(Vector2 v) : this(v.X, v.Y) { }

    public Vector2 ToVec2() => new(X, Z);
    public Vector3 ToVec3(float y = 0f) => new(X, y, Z);

    public static bool operator ==(WPos a, WPos b) => a.X == b.X && a.Z == b.Z;
    public static bool operator !=(WPos a, WPos b) => a.X != b.X || a.Z != b.Z;
    public static WPos operator +(WPos a, WDir b) => new(a.X + b.X, a.Z + b.Z);
    public static WPos operator +(WDir a, WPos b) => new(a.X + b.X, a.Z + b.Z);
    public static WPos operator -(WPos a, WDir b) => new(a.X - b.X, a.Z - b.Z);
    public static WDir operator -(WPos a, WPos b) => new(a.X - b.X, a.Z - b.Z);

    public bool AlmostEqual(WPos b, float eps) => MathF.Abs(X - b.X) <= eps && MathF.Abs(Z - b.Z) <= eps;
    public static WPos Lerp(WPos from, WPos to, float t) => new(from.X + (to.X - from.X) * t, from.Z + (to.Z - from.Z) * t);

    // --- containment helpers (used by AOE shapes) ---
    public bool InCircle(WPos center, float radius) => (this - center).LengthSq() <= radius * radius;
    public bool InDonut(WPos center, float inner, float outer) => InCircle(center, outer) && !InCircle(center, inner);
    public bool InCone(WPos apex, WDir dir, Angle halfAngle) => (this - apex).Normalized().Dot(dir) >= halfAngle.Cos();
    public bool InCone(WPos apex, Angle dir, Angle halfAngle) => InCone(apex, dir.ToDirection(), halfAngle);
    public bool InCircleCone(WPos apex, float radius, Angle dir, Angle halfAngle) => InCircle(apex, radius) && InCone(apex, dir, halfAngle);
    public bool InDonutCone(WPos apex, float inner, float outer, Angle dir, Angle halfAngle) => InDonut(apex, inner, outer) && InCone(apex, dir, halfAngle);
    // rect defined front/back extents along dir and half-width across it
    public bool InRect(WPos origin, Angle dir, float lenFront, float lenBack, float halfWidth) => (this - origin).InRect(dir.ToDirection(), lenFront, lenBack, halfWidth);

    public override string ToString() => $"[{X:f3}, {Z:f3}]";
    public bool Equals(WPos other) => this == other;
    public override bool Equals(object? obj) => obj is WPos other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Z);
}
