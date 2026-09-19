using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Minerva.Automation;
using Minerva.Modules;
using Minerva.Replay;

namespace Minerva.Windows;

/// <summary>
/// The main window shrunk to what can be read at a glance: is the AI on, what is it working from, what is it doing
/// right now, and is this being recorded. Modelled on SealBreaker's mini widget, which does the same job.
/// <para>The point is that it is small enough to leave on screen. Minimising the full window hides the state
/// entirely, and the state is the reason the window is open at all.</para>
/// </summary>
public sealed class MiniWindow : Window
{
    /// <summary>Wide enough that the state line does not reflow every time the wording changes.</summary>
    private const float MinContentWidth = 300f;

    private readonly Plugin plugin;
    private readonly ModuleManager manager;
    private readonly AIManager ai;
    private readonly ReplayService replay;

    public MiniWindow(Plugin plugin, ModuleManager manager, AIManager ai, ReplayService replay)
        : base("Minerva###MinervaMini",
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse)
    {
        this.plugin = plugin;
        this.manager = manager;
        this.ai = ai;
        this.replay = replay;
    }

    private (int Colors, int Vars) theme;

    public override void PreDraw() => this.theme = AegisTheme.Push();

    public override void PostDraw() => AegisTheme.Pop(this.theme);

    public override void Draw()
    {
        var cfg = this.plugin.Config;
        var active = this.manager.ActiveModuleInfo;
        var recording = this.replay.IsRecording;

        ImGui.Dummy(new Vector2(MinContentWidth, 0f));

        // what it is working from, and whether it may act
        UiKit.Pill(cfg.AutoDodgeEnabled ? "AI On" : "AI Off",
            cfg.AutoDodgeEnabled ? UiKit.Alpha(AegisTheme.Bronze, 0.7f) : AegisTheme.Rule,
            cfg.AutoDodgeEnabled ? AegisTheme.BronzeBright : AegisTheme.TravertineDim);
        ImGui.SameLine(0f, 6f);
        UiKit.Pill(active != null ? ModulesTab.BossName(active) : "Idle",
            active != null ? AegisTheme.TyrianBright : AegisTheme.Rule,
            active != null ? AegisTheme.Travertine : AegisTheme.TravertineDim);
        if (recording)
        {
            ImGui.SameLine(0f, 6f);
            UiKit.Pill("● REC", UiKit.Alpha(UiKit.Red, 0.6f), new Vector4(1f, 0.6f, 0.6f, 1f));
        }

        var (state, color) = this.DodgeState(cfg);
        ImGui.TextColored(color, state);

        ImGui.Spacing();

        if (UiKit.Toggle(recording ? "Stop" : "Record", recording, UiKit.Alpha(UiKit.Red, 0.18f), UiKit.Alpha(UiKit.Red, 0.7f)))
            this.plugin.ToggleRecording(showRecord: false);
        UiKit.Tip(recording ? "Stop recording and analyse it." : "Start recording this fight.");

        ImGui.SameLine(0f, 8f);
        var radar = ImGui.Button("Radar");
        UiKit.Tip("Show the radar.");
        if (radar)
            this.plugin.ShowRadar();

        // expand, right-aligned, as the last thing on the row
        ImGui.SameLine();
        var expandWidth = ImGui.GetFrameHeight() + 6f;
        var avail = ImGui.GetContentRegionAvail().X;
        if (avail > expandWidth)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - expandWidth);
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Expand))
            this.plugin.SwitchToFullWindow();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Back to the full window");
    }

    /// <summary>
    /// What the dodge is doing, in the radar's own words: the same four cases, so the two never disagree.
    /// </summary>
    private (string Text, Vector4 Color) DodgeState(Configuration cfg)
    {
        if (!cfg.AutoDodgeEnabled)
            return ("Auto-move off — guidance only.", AegisTheme.TravertineDim);
        if (this.ai.Movement is not MovementController mc)
            return ("No movement controller.", UiKit.Amber);
        if (mc.Steering)
            return ($"Steering to a safe spot ({mc.NavmeshBackend ?? "direct"}).", UiKit.Green);
        if (!mc.UsingNavmesh && !mc.HookInstalled)
            return ("No navmesh and no movement hook — check /xllog.", UiKit.Red);
        if (this.ai.HasSolution && this.ai.Current.NeedToMove && !this.ai.Current.Found)
            return ("In danger, but nowhere safe to stand.", UiKit.Amber);
        return ($"Idle ({mc.NavmeshBackend ?? "direct"}, nothing imminent).", AegisTheme.TravertineDim);
    }
}
