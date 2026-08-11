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

        ImGui.Spacing();
        ImGui.TextUnformatted("Auto-dodge");
        var guidance = cfg.AutoDodgeGuidance;
        if (ImGui.Checkbox("Show dodge guidance (safe-spot marker)", ref guidance)) { cfg.AutoDodgeGuidance = guidance; changed = true; }

        var autoMove = cfg.AutoDodgeEnabled;
        if (ImGui.Checkbox("Auto-move to safe spot", ref autoMove)) { cfg.AutoDodgeEnabled = autoMove; changed = true; }
        ImGui.SameLine();
        ImGui.TextDisabled("(no movement controller installed — guidance only)");

        if (changed)
            cfg.Save();
    }

    public void Dispose()
    {
    }
}
