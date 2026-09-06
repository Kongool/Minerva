using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace Minerva;

/// <summary>Which way is "up" on the radar.</summary>
/// <summary>
/// What a party marker's colour means. Mutually exclusive rather than a pair of toggles: a marker has one
/// colour, and two switches that both claim it would just mean one silently losing.
/// </summary>
public enum PartyColoring
{
    /// <summary>One colour for everyone.</summary>
    Uniform,

    /// <summary>Tank / healer / melee / ranged, in the game's own hues.</summary>
    Role,

    /// <summary>Light party 1 or 2 — for mechanics that split the party in half.</summary>
    Group,
}

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

    /// <summary>
    /// How opaque the radar's background is, 0 (see straight through to the game) to 1 (solid).
    /// <para>AOE fills are cut off at the field edge by geometry (ImGuiArena.FillClipped), so they stay inside
    /// the arena at any opacity. The paint-over mask still runs behind them for anything drawn with raw lines,
    /// and it is only as opaque as this setting.</para>
    /// </summary>
    public float RadarOpacity { get; set; } = 1f;

    /// <summary>Draw the rest of the party on the radar, not just yourself.</summary>
    public bool ShowPartyMembers { get; set; } = true;

    /// <summary>How party markers are coloured — see <see cref="PartyColoring"/>.</summary>
    public PartyColoring PartyColorBy { get; set; } = PartyColoring.Role;

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
    /// Show Minerva's two entries on the server info bar — the state readout and the auto-dodge toggle.
    /// On by default: they are the only place either answer is visible without opening a window.
    /// <para>Hides the entries rather than releasing the slots, so the order the user has sorted them into
    /// in Dalamud's own settings survives being switched off and back on.</para>
    /// </summary>
    public bool ShowDtrBar { get; set; } = true;

    /// <summary>
    /// How many yards of clearance the dodge aims to keep from an AOE edge. The dodge keeps moving until
    /// it is this far clear, so it accounts for hitbox radius, reaction time and server latency instead of
    /// stopping on the rim. 0 means "just leave the shape" — geometrically safe, but you can still be
    /// clipped in practice. Clamped to [0, 10].
    /// </summary>
    public float AutoDodgeSafetyMargin { get; set; } = 1f;

    /// <summary>
    /// How many seconds early the dodge aims to be clear of an AOE. The temporal twin of
    /// <see cref="AutoDodgeSafetyMargin"/>: that one buys distance from the edge, this one buys time.
    ///
    /// <para>Being clear exactly AS a mechanic resolves is not being safe. Measured across three fights:
    /// in Double Trouble the one charge that connected was left 0.39s before it landed while the tightest
    /// survivor was 0.53s, so the boundary between hit and clean sat under half a second — inside server
    /// latency. Every other clean dodge in that fight also finished in the 0.4-0.8s band, which is passing
    /// on luck rather than on margin.</para>
    ///
    /// <para>It works by solving as though it were already this far into the future, so a zone counts as
    /// imminent sooner AND the travel budget to escape it shrinks by the same amount. The cost is uptime:
    /// the character commits to leaving earlier and returns later. Clamped to [0, 5]; 0 restores the old
    /// behaviour of aiming to be clear exactly on resolution.</para>
    /// </summary>
    public float AutoDodgeClearanceLead { get; set; } = 1f;

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
