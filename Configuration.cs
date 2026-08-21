using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace Minerva;

/// <summary>Which way is "up" on the radar.</summary>
public enum RadarHeading
{
    /// <summary>North is always up. Stable, but needs mental rotation to map onto what you see.</summary>
    Static,

    /// <summary>Rotate so the camera's forward direction is up, matching the view on screen.</summary>
    CameraAlign,
}

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

    /// <summary>How the radar is oriented on screen.</summary>
    public RadarHeading RadarHeading { get; set; } = RadarHeading.Static;

    /// <summary>Show the auto-dodge target/arrow on the radar (guidance; you still move yourself).</summary>
    public bool AutoDodgeGuidance { get; set; } = true;

    /// <summary>
    /// Start recording by itself whenever a boss shows up — one with a module, or an unscripted one large
    /// enough to be a boss — and let the existing completion detector stop it. Off by default — recording is a deliberate act — but turning it on is how you accumulate a
    /// corpus to replay module changes against instead of re-clearing a dungeon by hand each time.
    /// </summary>
    public bool AutoRecordEncounters { get; set; }

    /// <summary>
    /// Guess dodges for content with no boss module — trash, open world — from enemy cast bars and the
    /// game's action data. Off by default: it is inference rather than an authored fight, so it can miss a
    /// mechanic the sheet does not describe.
    /// </summary>
    public bool AutoHintsForTrash { get; set; }

    /// <summary>Actually steer the character to the dodge target. Off by default; needs a movement controller.</summary>
    public bool AutoDodgeEnabled { get; set; }

    /// <summary>
    /// How many yards of clearance the dodge aims to keep from an AOE edge. The dodge keeps moving until
    /// it is this far clear, so it accounts for hitbox radius, reaction time and server latency instead of
    /// stopping on the rim. 0 means "just leave the shape" — geometrically safe, but you can still be
    /// clipped in practice. Clamped to [0, 10].
    /// </summary>
    public float AutoDodgeSafetyMargin { get; set; } = 1f;

    /// <summary>
    /// Which side of the target to stand on when the dodge has a free choice. Only a tiebreak: it never
    /// sends the character somewhere unsafe, and never across the arena, to satisfy a positional.
    /// </summary>
    public Positional DesiredPositional { get; set; } = Positional.Any;

    /// <summary>
    /// How far inside the requested arc to stand, in degrees off its nearest border.
    /// <para>BossmodReborn aims for the centre of the arc, which puts 45 degrees of travel between the flank
    /// and the rear. Jobs differ in whether they can afford that: Samurai has a full GCD between its flank
    /// and rear hits, Monk has one oGCD. Standing 15 degrees inside the border makes the switch a 15 degree
    /// turn while still being unambiguously on the requested side. Set 0 for BMR's behaviour.</para>
    /// </summary>
    public float PositionalArcMarginDeg { get; set; } = 15f;

    /// <summary>
    /// Turn the character away from gazes automatically. Off by default, and separate from auto-move: a
    /// gaze is the one mechanic where the answer is a facing rather than a position, and two characters
    /// cannot be turned by hand at once.
    /// </summary>
    public bool AutoFaceGazes { get; set; }

    /// <summary>When the Ariadne/vnavmesh plugin is loaded, route auto-move through its pathfinding instead
    /// of the raw walk override (paths around geometry). Falls back to the raw override when it's absent.</summary>
    public bool UseNavmesh { get; set; } = true;

    /// <summary>Leave other players (and pets) out of recordings — much smaller, cleaner logs, especially in
    /// open-field content. Your own character and party are kept. On by default.</summary>
    public bool RecordExcludeOtherPlayers { get; set; } = true;

    /// <summary>Saved dodge presets. The built-in Default is not stored here — it is always available.</summary>
    public List<DodgePreset> Presets { get; set; } = [];

    /// <summary>Which preset the live settings came from; see <see cref="DodgePresets"/>.</summary>
    public string ActivePreset { get; set; } = DodgePresets.DefaultName;

    public void Save() => Service.PluginInterface.SavePluginConfig(this);
}
