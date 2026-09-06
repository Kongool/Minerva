namespace Minerva;

using System.Reflection;

/// <summary>
/// The config-UI helpers ported modules name. Only the parts that need no ImGui live here — Minerva.Core
/// is Dalamud-free by design, and everything a module actually calls is reflection over the enum.
/// </summary>
public static class UICombo
{
    /// <summary>
    /// An enum member's human label: its <see cref="PropertyDisplayAttribute"/> if it carries one, else the
    /// member name. Modules use it to describe a role assignment to the player, so the fallback matters —
    /// an unlabelled member reads as "NorthTower", which is worse than a label and better than blank.
    /// </summary>
    public static string EnumString(Enum v)
    {
        var name = v.ToString();
        return v.GetType().GetField(name)?.GetCustomAttribute<PropertyDisplayAttribute>()?.Label ?? name;
    }
}
