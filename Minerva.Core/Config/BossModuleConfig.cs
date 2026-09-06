namespace Minerva;

/// <summary>
/// Display preferences the arena and ported modules read. Small on purpose: BossmodReborn's equivalent
/// carries its whole window UI, and Minerva only needs the parts a module actually asks about.
/// </summary>
[ConfigDisplay(Name = "Arena display")]
public sealed class BossModuleConfig : ConfigNode
{
    /// <summary>
    /// Outline arena text and draw shadows behind it.
    ///
    /// <para>On by default because a label sitting on a bright AOE is unreadable without one — the outline
    /// is what makes it legible, not decoration. A module reads this before drawing its own labels so the
    /// preference is honoured consistently rather than per-fight.</para>
    /// </summary>
    [PropertyDisplay("Outline arena text")]
    public bool ShowOutlinesAndShadows = true;
}
