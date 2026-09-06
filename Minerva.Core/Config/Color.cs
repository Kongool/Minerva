namespace Minerva;

/// <summary>
/// A colour a user can pick, stored as ABGR — the byte order ImGui packs into a <c>uint</c>. Ported from
/// BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt).
///
/// <para>Distinct from the raw <c>uint</c> colours in <see cref="Colors"/> so that the settings window can
/// tell a colour field from any other number and render a picker. Minerva's drawing takes RGBA, hence
/// <see cref="ToRGBA"/> at the boundary — silently mixing the two orders turns a translucent red into an
/// opaque blue, which is the sort of bug you spend an evening staring at.</para>
/// </summary>
public readonly struct Color(uint abgr)
{
    public readonly uint ABGR = abgr;

    public uint R => this.ABGR & 0xFF;
    public uint G => (this.ABGR >> 8) & 0xFF;
    public uint B => (this.ABGR >> 16) & 0xFF;
    public uint A => (this.ABGR >> 24) & 0xFF;

    public static Color FromComponents(uint r, uint g, uint b, uint a = 255)
        => new(((a & 0xFF) << 24) | ((b & 0xFF) << 16) | ((g & 0xFF) << 8) | (r & 0xFF));

    public static Color FromRGBA(uint rgba) => FromComponents(rgba >> 24, rgba >> 16, rgba >> 8, rgba);

    /// <summary>Back to the RGBA order <see cref="Colors"/> and the arena drawing use.</summary>
    public uint ToRGBA() => (this.R << 24) | (this.G << 16) | (this.B << 8) | this.A;

    public override string ToString() => $"#{this.R:X2}{this.G:X2}{this.B:X2}{this.A:X2}";
}
