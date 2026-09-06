namespace Minerva;

/// <summary>How a config node is presented in the settings window.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigDisplayAttribute : Attribute
{
    public string? Name { get; set; }
    public int Order { get; set; }

    /// <summary>Node this one nests under, so an expansion's fights group together.</summary>
    public Type? Parent { get; set; }

    public string[]? Tags { get; set; }
}

/// <summary>
/// Label and tooltip for one config field. Presence of this attribute is what makes a field appear in the
/// settings window at all — a public field without one is state the module keeps, not a choice the user makes.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PropertyDisplayAttribute(string label, uint color = default, string tooltip = "", bool separator = false, string[]? tags = null) : Attribute
{
    public string Label { get; } = label;
    public uint Color { get; } = color == default ? Colors.Text : color;
    public string Tooltip { get; } = tooltip;
    public bool Separator { get; } = separator;
    public string[] Tags { get; } = tags ?? [];
}

/// <summary>
/// Show an int or bool field as a dropdown of named choices rather than a checkbox. A bool takes two names,
/// false first — "Supports CW, DD CCW" reads as a strategy where "enabled" does not.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PropertyComboAttribute(string[] values) : Attribute
{
    public string[] Values { get; } = values;

    public PropertyComboAttribute(string falseText, string trueText) : this([falseText, trueText]) { }
}

/// <summary>Show a float or int field as a slider over the given range.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PropertySliderAttribute(float min, float max) : Attribute
{
    public float Speed { get; set; } = 1f;
    public float Min { get; } = min;
    public float Max { get; } = max;
    public bool Logarithmic { get; set; }
}

/// <summary>
/// Show an int-array field as a reorderable list of the given names — a priority order rather than eight
/// separate numbers.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PropertyStringOrderAttribute(string[] values) : Attribute
{
    public string[] Values { get; } = values;
}

/// <summary>
/// A group of related settings. Ported from BossmodReborn's <c>ConfigNode</c> (BSD-3; see
/// THIRD-PARTY-NOTICES.txt).
///
/// <para>Modules fetch their own node with <c>Service.Config.Get&lt;FRUConfig&gt;()</c> and read strategy
/// choices off it — which group goes north, which tank baits, whether to use the uptime variant. That is not
/// cosmetic: a module resolving a mechanic the opposite way from the party is worse than no module, so these
/// are the settings that make a raid module usable rather than decorative.</para>
///
/// <para>Fields are plain public fields, not properties, because the settings window reflects over them and
/// BossmodReborn's config files serialise them by name — keeping the shape identical is what lets a ported
/// config file load unchanged.</para>
/// </summary>
public abstract class ConfigNode
{
    /// <summary>Fired when any field changes, so the root can persist.</summary>
    public event Action<ConfigNode>? Modified;

    /// <summary>Announce a change; the root saves in response.</summary>
    public void NotifyModified() => this.Modified?.Invoke(this);
}
