using System;
using System.Text.RegularExpressions;

namespace Minerva.Generation;

/// <summary>
/// Reads shape facts out of an Omen row's VFX path. The Action sheet says a cast is a cone but not
/// how wide it is; the omen the action points at spells the angle in its filename
/// (<c>gl_fan120_1bf</c> = a 120-degree cone). Parsing lives here, in the game-free core, so it can be
/// tested against real path samples without the game or Lumina.
/// </summary>
public static partial class OmenPath
{
    // Two- and three-digit forms both occur in the wild: gl_fan060_1bf, er_gl_fan090_1bf,
    // m0070_fan180_0h, and bare two-digit ones like *_fan54_. Bespoke boss omens instead number
    // themselves fan01/fan02/fan03, so a value under 10 is a sequence index, not an angle.
    [GeneratedRegex(@"fan(\d{2,3})", RegexOptions.IgnoreCase)]
    private static partial Regex FanAngle();

    private const int MinAngleDeg = 10;
    private const int MaxAngleDeg = 360;

    /// <summary>
    /// Half-angle in degrees for a cone omen — what <c>AOEShapeCone</c> takes — or null when the path
    /// doesn't name one. The filename carries the *total* angle, so this halves it.
    /// </summary>
    public static float? ConeHalfAngleDeg(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        var m = FanAngle().Match(path);
        return m.Success && int.TryParse(m.Groups[1].ValueSpan, out var total) && total is >= MinAngleDeg and <= MaxAngleDeg
            ? total / 2f
            : null;
    }
}
