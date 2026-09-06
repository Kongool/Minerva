using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Minerva.Automation;
using Minerva.Modules;

namespace Minerva.Windows;

/// <summary>
/// The arena on its own: the field with its readout drawn over it, and a strip of the toggles you flip
/// mid-fight under it. Opens when a boss module activates and closes when it tears down, so in normal play
/// it is not on screen at all until it has something to say.
///
/// <para><b>Why this is separate from the main window.</b> The radar used to be one tab among five, which
/// made the thing you look at during a fight share a window with the thing you configure between them. A tab
/// bar you have to be on the right tab of is a poor way to find out an AOE is about to land, and the window
/// has to be sized for the settings pages rather than for the arena. Splitting them lets this one be small,
/// square and positioned where you want it.</para>
///
/// <para>The title bar stays: this window is movable and resizable, and hiding the chrome would leave no
/// way to do either. Turning it into a click-through overlay is a separate job — it needs the window to
/// stop taking input, which is a different setting from simply having no border.</para>
/// </summary>
public sealed class ArenaOverlayWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly AIManager ai;
    private readonly RadarView view;
    private readonly Configuration config;

    public ArenaOverlayWindow(Plugin plugin, ModuleManager manager, AIManager ai, Configuration config)
        : base("Minerva###MinervaArena")
    {
        this.plugin = plugin;
        this.ai = ai;
        this.config = config;
        this.view = new RadarView(manager, ai, config);
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(330, 340),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        this.Size = new Vector2(400, 460);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    private (int Colors, int Vars) theme;

    /// <summary>
    /// Push the theme, then override the window fill at the configured opacity so the field can be seen
    /// through. Pushed after the theme so it wins, and popped in <see cref="PostDraw"/>; the title bar keeps
    /// the theme's own colour, which is what makes the window still findable when the field is transparent.
    /// </summary>
    public override void PreDraw()
    {
        this.theme = AegisTheme.Push();
        ImGui.PushStyleColor(ImGuiCol.WindowBg, RadarView.BackgroundVec(this.config.RadarOpacity));
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor();
        AegisTheme.Pop(this.theme);
    }

    /// <summary>
    /// Close the moment the encounter ends, rather than waiting to be told.
    /// <para>The plugin drives opening from the module-activation edge, but nothing outside this window
    /// knows when the *view* has nothing left to draw. Without this the overlay lingers over the world
    /// showing "No active encounter" after every pull.</para>
    /// </summary>
    public override void Update()
    {
        if (this.IsOpen && this.config.AutoHideRadar && !this.view.HasEncounter)
            this.IsOpen = false;
    }

    public override void Draw()
    {
        var toolbar = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y + 2f;
        this.view.Draw(toolbar);
        this.DrawToolbar();
    }

    /// <summary>Every on/off you would flip during a pull, as one strip. Settings you tune live in the main window.</summary>
    private void DrawToolbar()
    {
        var cfg = this.config;
        var changed = false;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7f, 2f));

        if (UiKit.Toggle("AI", cfg.AutoDodgeEnabled)) { cfg.AutoDodgeEnabled = !cfg.AutoDodgeEnabled; changed = true; }
        UiKit.Tip("Auto-move to the safe spot");
        ImGui.SameLine(0f, 4f);
        if (UiKit.Toggle("Guide", cfg.AutoDodgeGuidance)) { cfg.AutoDodgeGuidance = !cfg.AutoDodgeGuidance; changed = true; }
        UiKit.Tip("Draw the safe spot");
        ImGui.SameLine(0f, 4f);
        if (UiKit.Toggle("Trash", cfg.AutoHintsForTrash)) { cfg.AutoHintsForTrash = !cfg.AutoHintsForTrash; changed = true; }
        UiKit.Tip("Also dodge unscripted content — trash, open world");
        ImGui.SameLine(0f, 4f);
        if (UiKit.Toggle("Face", cfg.AutoFaceGazes)) { cfg.AutoFaceGazes = !cfg.AutoFaceGazes; changed = true; }
        UiKit.Tip("Auto-face away from gazes");
        ImGui.SameLine(0f, 4f);
        if (UiKit.Toggle("Clip", cfg.ClipToArena)) { cfg.ClipToArena = !cfg.ClipToArena; changed = true; }
        UiKit.Tip("Confine AOEs to the arena");

        var recording = this.plugin.IsRecording;
        ImGui.SameLine(0f, 4f);
        if (UiKit.Toggle(recording ? "● REC" : "REC", recording, UiKit.Alpha(UiKit.Red, 0.18f), UiKit.Alpha(UiKit.Red, 0.7f)))
            this.plugin.ToggleRecording(showRecord: false);
        UiKit.Tip(recording ? "Stop & analyze" : "Start recording");

        var backend = this.ai.Movement is MovementController mc ? mc.NavmeshBackend ?? "direct" : "direct";
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), ImGui.GetWindowContentRegionMax().X - UiKit.PillWidth(backend)));
        UiKit.Pill(backend, AegisTheme.Rule, AegisTheme.TravertineDim);

        ImGui.PopStyleVar();
        if (changed)
            cfg.Save();
    }

    public void Dispose()
    {
    }
}
