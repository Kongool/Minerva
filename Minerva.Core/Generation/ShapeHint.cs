namespace Minerva.Generation;

public enum ShapeKind
{
    Unknown,
    Circle,
    Cone,
    Rect,
    Donut,
    Cross,
    SingleTarget, // no ground shape (tankbuster / gaze / etc.)

    /// <summary>A charge/dash: a rectangle whose WIDTH the sheet gives and whose LENGTH it cannot, because
    /// the length is how far the caster travels. Deliberately not <see cref="Rect"/> — emitting it as one
    /// would produce a fixed-length rect that is wrong every time the dash is any other distance.</summary>
    Charge,
}

/// <summary>
/// A best-effort shape for an action, resolved from game data (the Action sheet). Fields not
/// implied by the data are left at 0 with <see cref="NeedsReview"/> set, so the generator emits a
/// TODO rather than a wrong value. This is the seam between game-data lookups (plugin) and the
/// game-free generator (core).
/// </summary>
public readonly record struct ShapeHint(
    ShapeKind Kind,
    float Radius = 0f,
    float HalfAngleDeg = 0f,
    float HalfWidth = 0f,
    float InnerRadius = 0f,
    bool NeedsReview = false)
{
    public static readonly ShapeHint Unknown = new(ShapeKind.Unknown, NeedsReview: true);

    /// <summary>Build the actual <see cref="AOEShape"/> this hint describes, or null if unknown/single-target.</summary>
    public AOEShape? ToShape() => this.Kind switch
    {
        ShapeKind.Circle => new AOEShapeCircle(this.Radius),
        ShapeKind.Cone => new AOEShapeCone(this.Radius, this.HalfAngleDeg.Degrees()),
        ShapeKind.Rect => new AOEShapeRect(this.Radius, this.HalfWidth),
        ShapeKind.Donut => new AOEShapeDonut(this.InnerRadius, this.Radius),
        ShapeKind.Cross => new AOEShapeCross(this.Radius, this.HalfWidth),
        _ => null,
    };

    /// <summary>Emit the C# expression constructing the matching <c>AOEShape*</c>, or null if unknown.</summary>
    public string? ToShapeExpression()
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return this.Kind switch
        {
            ShapeKind.Circle => $"new AOEShapeCircle({F(this.Radius)}f)",
            ShapeKind.Cone => $"new AOEShapeCone({F(this.Radius)}f, {F(this.HalfAngleDeg)}f.Degrees())",
            ShapeKind.Rect => $"new AOEShapeRect({F(this.Radius)}f, {F(this.HalfWidth)}f)",
            ShapeKind.Donut => $"new AOEShapeDonut({F(this.InnerRadius)}f, {F(this.Radius)}f)",
            ShapeKind.Cross => $"new AOEShapeCross({F(this.Radius)}f, {F(this.HalfWidth)}f)",
            _ => null,
        };

        string F(float v) => v.ToString("0.###", inv);
    }
}

/// <summary>Resolves an action id to a shape hint, typically from game-data sheets.</summary>
public interface IShapeResolver
{
    ShapeHint Resolve(uint aid);
}

/// <summary>Fallback resolver that knows nothing — every action becomes a TODO.</summary>
public sealed class NullShapeResolver : IShapeResolver
{
    public ShapeHint Resolve(uint aid) => ShapeHint.Unknown;
}
