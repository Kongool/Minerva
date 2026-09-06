using Dalamud.Interface;
using Minerva;

namespace Minerva.Radar;

/// <summary>
/// Lets a ported module pass a <see cref="FontAwesomeIcon"/> straight to the arena.
///
/// <para>The primitive on <see cref="Arena"/> takes a <c>char</c>, because Minerva.Core is Dalamud-free and
/// cannot name Dalamud's enum. This bridges the two in the plugin assembly, where the enum is available.
/// Resolution is unambiguous: <c>FontAwesomeIcon</c> has no implicit conversion to <c>char</c>, so the
/// instance method is not a candidate and this extension is chosen.</para>
/// </summary>
public static class ArenaIconExtensions
{
    public static void IconWorld(this Arena arena, WPos center, FontAwesomeIcon icon, uint color, float fontSize = 17f)
        => arena.IconWorld(center, (char)icon, color, fontSize);
}
