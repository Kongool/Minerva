using System;
using System.Collections.Generic;
using System.Linq;

namespace Minerva;

/// <summary>
/// A named bundle of dodge behaviour.
/// <para>Presets carry how hard Minerva tries to keep you alive — nothing about identity or environment.
/// Whether the radar is on, which way it points, and whether you are recording all stay out, so a preset
/// applied by another plugin can never silently turn your radar off or end a recording mid-pull.</para>
/// </summary>
public sealed class DodgePreset
{
    public string Name { get; set; } = "";
    public bool AutoDodgeEnabled { get; set; }
    public bool AutoDodgeGuidance { get; set; } = true;
    public float AutoDodgeSafetyMargin { get; set; } = 1f;
    public bool AutoHintsForTrash { get; set; }
    public bool UseNavmesh { get; set; } = true;
    public float PositionalArcMarginDeg { get; set; } = 15f;

    public DodgePreset Copy(string? name = null) => new()
    {
        Name = name ?? this.Name,
        AutoDodgeEnabled = this.AutoDodgeEnabled,
        AutoDodgeGuidance = this.AutoDodgeGuidance,
        AutoDodgeSafetyMargin = this.AutoDodgeSafetyMargin,
        AutoHintsForTrash = this.AutoHintsForTrash,
        UseNavmesh = this.UseNavmesh,
        PositionalArcMarginDeg = this.PositionalArcMarginDeg,
    };

    /// <summary>Same behaviour, ignoring the name — used to tell "on a preset" from "edited since".</summary>
    public bool SameSettings(DodgePreset other)
        => this.AutoDodgeEnabled == other.AutoDodgeEnabled
        && this.AutoDodgeGuidance == other.AutoDodgeGuidance
        && Math.Abs(this.AutoDodgeSafetyMargin - other.AutoDodgeSafetyMargin) < 0.001f
        && this.AutoHintsForTrash == other.AutoHintsForTrash
        && this.UseNavmesh == other.UseNavmesh
        && Math.Abs(this.PositionalArcMarginDeg - other.PositionalArcMarginDeg) < 0.001f;
}

/// <summary>
/// Keeps the preset list, applies one to the live configuration, and records who asked for it.
/// <para>There is exactly one active slot. That is what lets another plugin (Daedalus drives BossmodReborn
/// this way) claim it, hand it back, and stay visible while it holds it — a plugin quietly writing
/// individual settings is indistinguishable from the user's own config drifting.</para>
/// <para>Whoever claims the slot must release it and nothing more. Writing settings back on release is
/// what makes two preset managers fight, so <see cref="Release"/> only acts if the caller still holds it.</para>
/// </summary>
public sealed class DodgePresets(Configuration config)
{
    public const string DefaultName = "Default";

    private readonly Configuration config = config;

    /// <summary>Plugin that claimed the active slot, or null when the user is driving.</summary>
    public string? Owner { get; private set; }

    /// <summary>Name of the preset currently applied. Never empty — there is always one.</summary>
    public string Active => this.config.ActivePreset;

    /// <summary>The built-in preset: guidance on, a yard of clearance, no unscripted-content guessing.</summary>
    public static DodgePreset Default => new()
    {
        Name = DefaultName,
        AutoDodgeEnabled = false,
        AutoDodgeGuidance = true,
        AutoDodgeSafetyMargin = 1f,
        AutoHintsForTrash = false,
        UseNavmesh = true,
        PositionalArcMarginDeg = 15f,
    };

    /// <summary>Every preset, the built-in first. The default is always present and cannot be removed.</summary>
    public List<DodgePreset> All()
    {
        var all = new List<DodgePreset> { Default };
        foreach (var p in this.config.Presets)
            if (!string.Equals(p.Name, DefaultName, StringComparison.OrdinalIgnoreCase))
                all.Add(p);
        return all;
    }

    public DodgePreset? Find(string name)
        => this.All().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The live settings as a preset, so they can be compared or saved.</summary>
    public DodgePreset Current() => new()
    {
        Name = this.config.ActivePreset,
        AutoDodgeEnabled = this.config.AutoDodgeEnabled,
        AutoDodgeGuidance = this.config.AutoDodgeGuidance,
        AutoDodgeSafetyMargin = this.config.AutoDodgeSafetyMargin,
        AutoHintsForTrash = this.config.AutoHintsForTrash,
        UseNavmesh = this.config.UseNavmesh,
        PositionalArcMarginDeg = this.config.PositionalArcMarginDeg,
    };

    /// <summary>True when the live settings no longer match the preset they came from.</summary>
    public bool Modified => this.Find(this.config.ActivePreset) is { } p && !p.SameSettings(this.Current());

    /// <summary>
    /// Copy a preset into the live configuration. <paramref name="owner"/> names the plugin claiming the
    /// slot, or null for the user.
    /// </summary>
    public bool Apply(string name, string? owner = null)
    {
        if (this.Find(name) is not { } preset)
            return false;

        this.config.AutoDodgeEnabled = preset.AutoDodgeEnabled;
        this.config.AutoDodgeGuidance = preset.AutoDodgeGuidance;
        this.config.AutoDodgeSafetyMargin = Math.Clamp(preset.AutoDodgeSafetyMargin, 0f, 10f);
        this.config.AutoHintsForTrash = preset.AutoHintsForTrash;
        this.config.UseNavmesh = preset.UseNavmesh;
        this.config.PositionalArcMarginDeg = Math.Clamp(preset.PositionalArcMarginDeg, 0f, 44f);
        this.config.ActivePreset = preset.Name;
        this.Owner = owner;
        this.config.Save();
        return true;
    }

    /// <summary>
    /// Save the live settings as a preset. An existing name is overwritten, which is what "save" means to
    /// someone who has just adjusted a slider on a preset they already have.
    /// </summary>
    public bool Save(string name)
    {
        name = name.Trim();
        if (name.Length == 0 || string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase))
            return false; // the built-in is the fallback; letting it be overwritten leaves nothing to return to

        var preset = this.Current().Copy(name);
        var at = this.config.Presets.FindIndex(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (at >= 0)
            this.config.Presets[at] = preset;
        else
            this.config.Presets.Add(preset);

        this.config.ActivePreset = name;
        this.Owner = null;
        this.config.Save();
        return true;
    }

    public bool Delete(string name)
    {
        if (string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase))
            return false;

        var removed = this.config.Presets.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed && string.Equals(this.config.ActivePreset, name, StringComparison.OrdinalIgnoreCase))
            this.Apply(DefaultName);
        else if (removed)
            this.config.Save();
        return removed;
    }

    /// <summary>
    /// Give the slot back. Only the holder may release it: a caller that has already been superseded must
    /// not undo whatever replaced it, or two managers end up fighting over the slot several times a second.
    /// </summary>
    public bool Release(string owner)
    {
        if (this.Owner != owner)
            return false;
        this.Owner = null;
        return this.Apply(DefaultName);
    }
}
