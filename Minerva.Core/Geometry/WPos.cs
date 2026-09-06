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

    // BMR compatibility: Minerva does not quantize world positions, so these are identity/simple rounding
    public WPos Quantized() => this;
    public WPos Rounded(float precision = 1f) => new(MathF.Round(X / precision) * precision, MathF.Round(Z / precision) * precision);

    /// <summary>Rotate <paramref name="point"/> about <paramref name="origin"/> by the given degrees (BMR helper).</summary>
    public static WPos RotateAroundOrigin(float rotateByDegrees, WPos origin, WPos point)
    {
        var a = rotateByDegrees * Angle.DegToRad;
        var (sin, cos) = MathF.SinCos(a);
        var d = point - origin;
        return origin + new WDir(d.X * cos - d.Z * sin, d.X * sin + d.Z * cos);
    }

    /// <summary>Rotate every vertex about <paramref name="center"/> by the given degrees (BMR helper).</summary>
    public static WPos[] GenerateRotatedVertices(WPos center, WPos[] vertices, float rotationAngle)
    {
        var result = new WPos[vertices.Length];
        for (var i = 0; i < vertices.Length; ++i)
            result[i] = RotateAroundOrigin(rotationAngle, center, vertices[i]);
        return result;
    }

    // --- containment helpers (used by AOE shapes) ---
    public bool InCircle(WPos center, float radius) => (this - center).LengthSq() <= radius * radius;
    /// <summary>Inside an axis-aligned square of side 2*halfSize. Ported modules use it for arena quadrants.</summary>
    public bool InSquare(WPos center, float halfSize)
        => MathF.Abs(this.X - center.X) <= halfSize && MathF.Abs(this.Z - center.Z) <= halfSize;

    /// <summary>Inside a square of side 2*halfSize turned to face <paramref name="dir"/>.</summary>
    /// <remarks>The axis-aligned form above is the common case; a turned square shows up wherever a fight
    /// draws tiles at an angle to the arena, and testing it axis-aligned silently accepts the corners.</remarks>
    public bool InSquare(WPos center, float halfSize, WDir dir)
    {
        var off = this - center;
        var along = off.Dot(dir);
        var across = off.Dot(dir.OrthoL());
        return MathF.Abs(along) <= halfSize && MathF.Abs(across) <= halfSize;
    }

    /// <inheritdoc cref="InSquare(WPos,float,WDir)"/>
    public bool InSquare(WPos center, float halfSize, Angle rotation) => this.InSquare(center, halfSize, rotation.ToDirection());

    public bool InDonut(WPos center, float inner, float outer) => InCircle(center, outer) && !InCircle(center, inner);
    public bool InCone(WPos apex, WDir dir, Angle halfAngle) => (this - apex).Normalized().Dot(dir) >= halfAngle.Cos();
    public bool InCone(WPos apex, Angle dir, Angle halfAngle) => InCone(apex, dir.ToDirection(), halfAngle);
    public bool InCircleCone(WPos apex, float radius, Angle dir, Angle halfAngle) => InCircle(apex, radius) && InCone(apex, dir, halfAngle);
    public bool InDonutCone(WPos apex, float inner, float outer, Angle dir, Angle halfAngle) => InDonut(apex, inner, outer) && InCone(apex, dir, halfAngle);
    // rect defined front/back extents along dir and half-width across it
    public bool InRect(WPos origin, Angle dir, float lenFront, float lenBack, float halfWidth) => (this - origin).InRect(dir.ToDirection(), lenFront, lenBack, halfWidth);

    /// <summary>As above, given the facing as a direction rather than an angle. Modules that already
    /// hold a direction vector spell it this way rather than round-tripping through an angle.</summary>
    public bool InRect(WPos origin, WDir dir, float lenFront, float lenBack, float halfWidth) => (this - origin).InRect(dir, lenFront, lenBack, halfWidth);

    /// <summary>This position read as an offset from the origin. Used where a polygon stores vertices
    /// relative to its centre and a world position has to become one of them.</summary>
    public WDir ToWDir() => new(this.X, this.Z);

    /// <summary>Inside the rectangle running from <paramref name="origin"/> along the full length of
    /// <paramref name="extent"/> — the shape a charge between two points sweeps.</summary>
    /// <summary>Inside an axis-aligned box centred on <paramref name="origin"/>. Half-extents, not corners —
    /// this is the arena-boundary test knockback survival is decided against.</summary>
    public bool InRect(WPos origin, float halfWidth, float halfHeight)
        => MathF.Abs(this.X - origin.X) <= halfWidth && MathF.Abs(this.Z - origin.Z) <= halfHeight;

    /// <summary>Inside the corridor from <paramref name="origin"/> to <paramref name="end"/>.</summary>
    public bool InRect(WPos origin, WPos end, float halfWidth) => this.InRect(origin, end - origin, halfWidth);

    public bool InRect(WPos origin, WDir extent, float halfWidth)
    {
        var len = extent.Length();
        return len > 0f && this.InRect(origin, extent / len, len, 0f, halfWidth);
    }

    public override string ToString() => $"[{X:f3}, {Z:f3}]";
    public bool Equals(WPos other) => this == other;
    public override bool Equals(object? obj) => obj is WPos other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Z);
}

/// <summary>Conversions between the game's 3D vectors and Minerva's XZ-plane position.</summary>
public static class Vector3Extensions
{
    /// <summary>Drop the vertical component: everything Minerva reasons about is on the floor plane.</summary>
    public static WPos ToWPos(this Vector3 v) => new(v.X, v.Z);

    /// <summary>The horizontal components as a plain <see cref="Vector2"/>, matching BossmodReborn.
    /// Ported code feeds the result straight into <c>new WPos(...)</c>, so the type has to be Vector2
    /// rather than WPos.</summary>
    public static Vector2 XZ(this Vector3 v) => new(v.X, v.Z);

    public static Vector4 ToVec4(this WPos p, float y = 0f, float rotation = 0f) => new(p.X, y, p.Z, rotation);

    /// <summary>As a 4-component vector. Same purpose as the <see cref="WPos"/> overload — writing a
    /// computed value into an actor's <c>PosRot</c> — for the cases where the module has an offset rather
    /// than a position.</summary>
    public static Vector4 ToVec4(this WDir d, float y = 0f, float rotation = 0f) => new(d.X, y, d.Z, rotation);
}
