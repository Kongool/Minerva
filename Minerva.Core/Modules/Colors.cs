namespace Minerva;

/// <summary>
/// Palette shared by modules and the radar. Values are packed the way ImGui expects
/// (0xAABBGGRR — alpha, blue, green, red), so the renderer can use them directly.
/// </summary>
public static class Colors
{
    public static uint RGBA(byte r, byte g, byte b, byte a) => (uint)(r | (g << 8) | (b << 16) | (a << 24));

    public static readonly uint AOE = RGBA(255, 0, 0, 90);        // translucent red fill (future danger)
    public static readonly uint AOEImminent = RGBA(255, 40, 0, 160); // brighter fill for an AOE about to resolve
    public static readonly uint Danger = RGBA(255, 0, 0, 255);    // bright red outline
    public static readonly uint Safe = RGBA(0, 200, 0, 70);       // translucent green
    public static readonly uint Border = RGBA(255, 255, 255, 255);
    public static readonly uint Shadow = RGBA(0, 0, 0, 200);
    public static readonly uint PC = RGBA(255, 140, 0, 255);     // local player (orange)
    public static readonly uint Player = RGBA(120, 120, 255, 255);// other party members
    public static readonly uint Enemy = RGBA(255, 0, 255, 255);   // primary/boss actor
    public static readonly uint Object = RGBA(255, 210, 0, 255);  // helpers/objects
    public static readonly uint Text = RGBA(255, 255, 255, 255);
}
