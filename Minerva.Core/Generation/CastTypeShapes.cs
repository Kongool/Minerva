namespace Minerva.Generation;

/// <summary>
/// Turns an Action row's <c>CastType</c> into a shape.
/// <para>The numbering is not guessable and not documented — 8 is a charge, 10 is a donut, 7 is a
/// ground-targeted circle — so it is a table to be copied, not derived. It lived in the plugin project
/// behind a Lumina sheet, which meant no test could reach it, and two entries were wrong for as long as
/// the generator existed: donuts fell into the rectangle bucket and came out as
/// <c>AOEShapeRect(range, XAxisModifier / 2)</c>. A donut carries no <c>XAxisModifier</c>, so the width
/// was zero and the emitted shape could never contain anything.</para>
/// <para>Values match BossmodReborn's <c>AIHintsBuilder.GuessShape</c>, which is the only reading of
/// these numbers with real mileage behind it.</para>
/// </summary>
public static class CastTypeShapes
{
    /// <summary>Fallback when a cone's omen does not name its angle. Total 90 degrees.</summary>
    public const float DefaultConeHalfAngleDeg = 45f;

    /// <summary>
    /// A donut's inner radius appears in no sheet, so it is estimated and flagged for review. A fraction
    /// rather than a constant because inner and outer scale together in practice.
    /// </summary>
    public const float DonutInnerFraction = 0.4f;

    /// <param name="omenPath">The action's Omen row path, which encodes a cone's angle by name
    /// (<c>gl_fan120_1bf</c> = 120 degrees total). Null or unparseable falls back and flags review.</param>
    public static ShapeHint Resolve(byte castType, float effectRange, float xAxisModifier, string? omenPath)
    {
        var halfWidth = xAxisModifier / 2f;
        return castType switch
        {
            1 => new ShapeHint(ShapeKind.SingleTarget),
            2 or 5 => new ShapeHint(ShapeKind.Circle, Radius: effectRange),

            // BMR leaves 6 commented as "custom shapes". A circle is the common case and the best guess
            // available, but it is a guess, so it goes out with a TODO rather than silently.
            6 => new ShapeHint(ShapeKind.Circle, Radius: effectRange, NeedsReview: true),

            7 => new ShapeHint(ShapeKind.Circle, Radius: effectRange),   // ground-targeted circle, NOT a rect
            3 or 13 => Cone(effectRange, omenPath),
            4 or 12 => new ShapeHint(ShapeKind.Rect, Radius: effectRange, HalfWidth: halfWidth),

            // a charge's length is how far the caster travels, so there is no radius to give here
            8 => new ShapeHint(ShapeKind.Charge, HalfWidth: halfWidth, NeedsReview: true),

            10 => new ShapeHint(ShapeKind.Donut, Radius: effectRange,
                InnerRadius: MathF.Round(effectRange * DonutInnerFraction), NeedsReview: true),
            11 => new ShapeHint(ShapeKind.Cross, Radius: effectRange, HalfWidth: halfWidth, NeedsReview: true),
            _ => ShapeHint.Unknown,
        };
    }

    private static ShapeHint Cone(float range, string? omenPath)
        => omenPath != null && OmenPath.ConeHalfAngleDeg(omenPath) is { } halfAngle
            ? new ShapeHint(ShapeKind.Cone, Radius: range, HalfAngleDeg: halfAngle)
            : new ShapeHint(ShapeKind.Cone, Radius: range, HalfAngleDeg: DefaultConeHalfAngleDeg, NeedsReview: true);
}
