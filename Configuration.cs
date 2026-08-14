using System;
using Dalamud.Configuration;

namespace Minerva;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Master switch for the arena radar overlay (drawn from Phase 3 onward).</summary>
    public bool RadarEnabled { get; set; } = true;

    /// <summary>Auto-open the radar window when a known boss module activates (BMR-style pull behaviour).</summary>
    public bool AutoShowRadar { get; set; } = true;

    /// <summary>Auto-close the radar window when the active module tears down (boss dead / left the duty).
    /// Off by default so the window stays up for post-pull review, matching BMR.</summary>
    public bool AutoHideRadar { get; set; }

    /// <summary>Mask AOE fills that extend past the arena boundary so danger zones are confined to the field.</summary>
    public bool ClipToArena { get; set; } = true;

    /// <summary>Rotate the radar so the local player's facing points up (heading-up), instead of north-up.</summary>
    public bool RotateRadar { get; set; }

    /// <summary>Show the auto-dodge target/arrow on the radar (guidance; you still move yourself).</summary>
    public bool AutoDodgeGuidance { get; set; } = true;

    /// <summary>Actually steer the character to the dodge target. Off by default; needs a movement controller.</summary>
    public bool AutoDodgeEnabled { get; set; }

    /// <summary>Leave other players (and pets) out of recordings — much smaller, cleaner logs, especially in
    /// open-field content. Your own character and party are kept. On by default.</summary>
    public bool RecordExcludeOtherPlayers { get; set; } = true;

    public void Save() => Service.PluginInterface.SavePluginConfig(this);
}
