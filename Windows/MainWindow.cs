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

    public override void Draw() => this.DrawContent();

    /// <summary>The menu body, factored out so it can also be embedded as a tab in the radar window.</summary>
    public void DrawContent()
    {
        ImGui.TextUnformatted("Minerva");
        ImGui.TextDisabled("/minerva radar · debug · replay · record · menu");
        ImGui.Separator();

        // window/tool toggles — same actions as the /minerva subcommands
        ImGui.TextUnformatted("Windows");
        if (ImGui.Button("Radar")) this.plugin.ToggleRadar();
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

        var rotate = cfg.RotateRadar;
        if (ImGui.Checkbox("Rotate radar with my character (heading-up)", ref rotate)) { cfg.RotateRadar = rotate; changed = true; }

        ImGui.Spacing();
        ImGui.TextUnformatted("Auto-dodge");
        var guidance = cfg.AutoDodgeGuidance;
        if (ImGui.Checkbox("Show dodge guidance (safe-spot marker)", ref guidance)) { cfg.AutoDodgeGuidance = guidance; changed = true; }

        var autoMove = cfg.AutoDodgeEnabled;
        if (ImGui.Checkbox("Auto-move to safe spot", ref autoMove)) { cfg.AutoDodgeEnabled = autoMove; changed = true; }
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.2f, 1f), "(experimental — steers your character)");

        var useNav = cfg.UseNavmesh;
        if (ImGui.Checkbox("Use navmesh (Ariadne/vnavmesh) when available", ref useNav)) { cfg.UseNavmesh = useNav; changed = true; }
        ImGui.SameLine();
        ImGui.TextDisabled("(paths around walls; falls back to direct steering)");

        if (changed)
            cfg.Save();
    }

    public void Dispose()
    {
    }
}
