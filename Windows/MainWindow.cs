using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Minerva.Windows;

/// <summary>
/// Placeholder main window for Phase 0. Confirms the plugin loads and draws.
/// Real content (radar, module list, config) arrives in later phases — see PLAN.md.
/// </summary>
public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string presetName = string.Empty;

    public MainWindow(Plugin plugin)
        : base("Minerva###MinervaMain")
    {
        this.plugin = plugin;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    private (int Colors, int Vars) theme;

    public override void PreDraw() => this.theme = AegisTheme.Push();

    public override void PostDraw() => AegisTheme.Pop(this.theme);


    public override void Draw() => this.DrawContent();

    /// <summary>The menu body, factored out so it can also be embedded as a tab in the radar window.</summary>
    public void DrawContent()
    {
        ImGui.TextUnformatted("Minerva");
        ImGui.TextDisabled("/minerva radar · debug · replay · record · menu");
        ImGui.Separator();

        // window/tool toggles — same actions as the /minerva subcommands
        ImGui.TextUnformatted("Windows");
        if (ImGui.Button("Radar")) this.plugin.ShowRadar();
        ImGui.SameLine();
        if (ImGui.Button("Debug inspector")) this.plugin.ToggleDebug();
        ImGui.SameLine();
        if (ImGui.Button("Sandbox")) this.plugin.ToggleSandbox();

        if (ImGui.Button("Replay")) this.plugin.ToggleReplay();
        ImGui.SameLine();
        var recording = this.plugin.IsRecording;
        if (ImGui.Button(recording ? "Stop recording" : "Start recording")) this.plugin.ToggleRecording();
        if (recording)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "● REC");
        }

        ImGui.Separator();

        var cfg = this.plugin.Config;
        var changed = false;

        var radar = cfg.RadarEnabled;
        if (ImGui.Checkbox("Radar overlay", ref radar)) { cfg.RadarEnabled = radar; changed = true; }

        var autoShow = cfg.AutoShowRadar;
        if (ImGui.Checkbox("Auto-open radar on pull", ref autoShow)) { cfg.AutoShowRadar = autoShow; changed = true; }
        ImGui.SameLine();
        ImGui.TextDisabled("(pops the radar when a known boss module activates)");

        var autoHide = cfg.AutoHideRadar;
        if (ImGui.Checkbox("Auto-close radar when the fight ends", ref autoHide)) { cfg.AutoHideRadar = autoHide; changed = true; }

        var clip = cfg.ClipToArena;
        if (ImGui.Checkbox("Confine AOEs to the arena", ref clip)) { cfg.ClipToArena = clip; changed = true; }

        var autoRec = cfg.AutoRecordEncounters;
        if (ImGui.Checkbox("Auto-record boss encounters", ref autoRec)) { cfg.AutoRecordEncounters = autoRec; changed = true; }
        ImGui.SameLine();
        ImGui.TextDisabled("(also records bosses with no module yet — stops itself when the fight ends)");

        ImGui.Spacing();
        ImGui.TextUnformatted("Radar heading");
        var heading = (int)cfg.RadarHeading;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo("##radarheading", ref heading, "Static (north up)\0Camera align\0"))
        {
            cfg.RadarHeading = (RadarHeading)heading;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Auto-dodge");
        this.DrawPresets(cfg);
        var guidance = cfg.AutoDodgeGuidance;
        if (ImGui.Checkbox("Show dodge guidance (safe-spot marker)", ref guidance)) { cfg.AutoDodgeGuidance = guidance; changed = true; }

        var trash = cfg.AutoHintsForTrash;
        if (ImGui.Checkbox("Also dodge unscripted content (trash, open world)", ref trash)) { cfg.AutoHintsForTrash = trash; changed = true; }
        ImGui.SameLine();
        ImGui.TextDisabled("(guesses from enemy casts when no boss module is active)");

        var autoMove = cfg.AutoDodgeEnabled;
        if (ImGui.Checkbox("Auto-move to safe spot", ref autoMove)) { cfg.AutoDodgeEnabled = autoMove; changed = true; }
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.2f, 1f), "(experimental — steers your character)");

        var margin = cfg.AutoDodgeSafetyMargin;
        ImGui.SetNextItemWidth(160f);
        if (ImGui.SliderFloat("Safety margin (yards)", ref margin, 0f, 10f, "%.1f"))
        {
            cfg.AutoDodgeSafetyMargin = Math.Clamp(margin, 0f, 10f);
            changed = true;
        }
        ImGui.SameLine();
        if (cfg.AutoDodgeSafetyMargin <= 0f)
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.2f, 1f), "! at 0y you may still get hit at times");
        else
            ImGui.TextDisabled("(clearance kept from the AOE edge)");

        // a set rather than a choice: with no free hand to hold an override key, the acceptable sides have
        // to be stated up front, and most melee are happy with rear OR flank
        ImGui.TextUnformatted("Acceptable sides");
        ImGui.SameLine();
        ImGui.TextDisabled("(none ticked = anywhere; a tiebreak only, never worth a death)");
        foreach (var (label, flag) in new[] { ("Front", Positional.Front), ("Flank", Positional.Flank), ("Rear", Positional.Rear) })
        {
            var on = (cfg.DesiredPositional & flag) != 0;
            if (ImGui.Checkbox(label, ref on))
            {
                cfg.DesiredPositional = on ? cfg.DesiredPositional | flag : cfg.DesiredPositional & ~flag;
                changed = true;
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();

        // BMR aims for the centre of the arc, so a flank-to-rear switch is 45 degrees of travel. Standing
        // just inside the border makes it 15 -- which matters for jobs with only an oGCD between the two.
        var arc = cfg.PositionalArcMarginDeg;
        ImGui.SetNextItemWidth(160f);
        if (ImGui.SliderFloat("Stand this far inside the arc", ref arc, 0f, 44f, "%.0f deg"))
        {
            cfg.PositionalArcMarginDeg = Math.Clamp(arc, 0f, 44f);
            changed = true;
        }

        ImGui.SameLine();
        if (cfg.PositionalArcMarginDeg <= 0f)
            ImGui.TextDisabled("(0 = dead centre of the arc, as BossmodReborn does it)");
        else
            ImGui.TextDisabled("(shorter turn when the next hit wants the neighbouring side)");

        var face = cfg.AutoFaceGazes;
        if (ImGui.Checkbox("Auto-face away from gazes", ref face)) { cfg.AutoFaceGazes = face; changed = true; }
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.2f, 1f), "(experimental — turns your character)");

        var useNav = cfg.UseNavmesh;
        if (ImGui.Checkbox("Use navmesh (Ariadne/vnavmesh) when available", ref useNav)) { cfg.UseNavmesh = useNav; changed = true; }
        ImGui.SameLine();
        ImGui.TextDisabled("(paths around walls; falls back to direct steering)");

        if (changed)
            cfg.Save();
    }

    /// <summary>
    /// Preset row: pick one, save the current settings as a new one, or delete a saved one.
    /// <para>It also shows when another plugin holds the slot, and when the live settings have drifted
    /// from the preset they came from. Both are states a user would otherwise read as Minerva having
    /// forgotten their configuration.</para>
    /// </summary>
    private void DrawPresets(Configuration cfg)
    {
        var presets = this.plugin.Presets;
        var all = presets.All();
        var names = new string[all.Count];
        var current = 0;
        for (var i = 0; i < all.Count; ++i)
        {
            names[i] = all[i].Name;
            if (string.Equals(all[i].Name, cfg.ActivePreset, StringComparison.OrdinalIgnoreCase))
                current = i;
        }

        ImGui.SetNextItemWidth(180f);
        if (ImGui.Combo("Preset", ref current, names, names.Length))
            presets.Apply(names[current]);

        ImGui.SameLine();
        if (presets.Owner is { } owner)
            ImGui.TextColored(new Vector4(0.72f, 0.51f, 0.94f, 1f), $"held by {owner}");
        else if (presets.Modified)
            ImGui.TextDisabled("(edited - save to keep)");

        ImGui.SetNextItemWidth(180f);
        ImGui.InputTextWithHint("##presetname", "new preset name", ref this.presetName, 48);
        ImGui.SameLine();
        if (ImGui.Button("Save as") && presets.Save(this.presetName))
            this.presetName = string.Empty;

        var isBuiltIn = string.Equals(cfg.ActivePreset, DodgePresets.DefaultName, StringComparison.OrdinalIgnoreCase);
        ImGui.SameLine();
        if (isBuiltIn)
        {
            ImGui.BeginDisabled();
            ImGui.Button("Delete");
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Default is the fallback every other preset returns to, so it stays.");
        }
        else if (ImGui.Button("Delete"))
        {
            presets.Delete(cfg.ActivePreset);
        }

        ImGui.Spacing();
    }

    public void Dispose()
    {
    }
}
