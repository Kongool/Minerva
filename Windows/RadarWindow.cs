using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Minerva;
using Minerva.Automation;
using Minerva.Modules;
using Minerva.Radar;

namespace Minerva.Windows;

/// <summary>
/// The radar: draws the active module's arena (danger zones, boundary, actors) and its hints. When
/// no module is active it shows a short status line. This is the player-facing payoff of Phases 1–3.
/// </summary>
public sealed class RadarWindow : Window, IDisposable
{
    private readonly ModuleManager manager;
    private readonly AIManager ai;
    private readonly Configuration config;
    private readonly WorldStateDebugWindow debug;
    private readonly MainWindow menu;
    private readonly ReplayWindow replay;
    private readonly ImGuiArena arena = new();

    public RadarWindow(ModuleManager manager, AIManager ai, Configuration config, WorldStateDebugWindow debug, MainWindow menu, ReplayWindow replay)
        : base("Minerva Radar###MinervaRadar")
    {
        this.manager = manager;
        this.ai = ai;
        this.config = config;
        this.debug = debug;
        this.menu = menu;
        this.replay = replay;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 360),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        if (!ImGui.BeginTabBar("###radartabs"))
            return;

        this.Tab("Radar", this.DrawRadar);
        this.Tab("Menu", this.menu.DrawContent);
        this.Tab("Replay", this.replay.DrawContent);
        this.Tab("Debug", this.debug.DrawContent);

        ImGui.EndTabBar();
    }

    /// <summary>
    /// Draw one tab with its content guarded. If the content throws, we still run EndTabItem so ImGui's
    /// tab/ID stack stays balanced — an unbalanced stack is what turns a draw bug into a hard crash that
    /// takes the whole plugin down. The exception is shown in-tab and logged once so it can be fixed.
    /// </summary>
    private void Tab(string label, Action content)
    {
        if (!ImGui.BeginTabItem(label))
            return;
        try
        {
            content();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), $"{label} draw error: {ex.Message}");
            Service.Log.Error(ex, $"Minerva: {label} tab draw threw.");
        }
        finally
        {
            ImGui.EndTabItem();
        }
    }

    private void DrawRadar()
    {
        var module = this.manager.ActiveModule;
        if (module == null)
        {
            ImGui.TextUnformatted("No active encounter.");
            ImGui.TextDisabled($"{this.manager.RegisteredCount} module(s) registered. Waiting for a known boss.");
            return;
        }

        var pc = this.manager.LocalPlayer();
        var pcSlot = 0;

        // hints above the arena
        var global = new ModuleComponent.GlobalHints();
        module.AddGlobalHints(global);
        foreach (var h in global)
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), h);

        if (pc != null)
        {
            var hints = new ModuleComponent.TextHints();
            module.AddHints(pcSlot, pc, hints);
            foreach (var (text, risk) in hints)
                ImGui.TextColored(risk ? new Vector4(1f, 0.3f, 0.3f, 1f) : new Vector4(0.8f, 0.8f, 0.8f, 1f), text);
        }

        // auto-move diagnostics: makes it obvious why steering may not be happening
        if (this.config.AutoDodgeEnabled && this.ai.Movement is MovementController mc)
        {
            var backend = mc.NavmeshBackend ?? "direct";
            if (mc.Steering)
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), $"Auto-move: steering to safe spot ({backend})");
            else if (!mc.UsingNavmesh && !mc.HookInstalled)
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Auto-move: no navmesh and movement hook NOT installed (signature outdated) — check /xllog");
            else if (this.ai.HasSolution && this.ai.Current.NeedToMove && !this.ai.Current.Found)
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), "Auto-move: in danger but no safe spot found");
            else
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), $"Auto-move: idle ({backend}, no imminent danger)");
        }

        // arena canvas
        var canvasTopLeft = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        var side = MathF.Min(canvasSize.X, canvasSize.Y);
        if (side < 32f)
            return;
        canvasSize = new Vector2(side, side);
        ImGui.InvisibleButton("##canvas", canvasSize); // reserve the region

        this.arena.Center = module.Center;
        this.arena.Bounds = module.Bounds;
        // heading-up: rotate so the player's facing points up (φ = rotation + π); 0 = north-up
        this.arena.Rotation = this.config.RotateRadar && pc != null ? pc.Rotation.Rad + MathF.PI : 0f;
        this.arena.Begin(canvasTopLeft, canvasSize);

        module.Arena = this.arena;

        // draw the local player on top of module content via the foreground pass; module draws the rest
        module.DrawArena(pcSlot, pc ?? module.PrimaryActor);

        // confine danger zones to the field: mask everything past the boundary, then restroke the border
        if (this.config.ClipToArena)
        {
            this.arena.ClipOutsideArena(ImGui.GetColorU32(ImGuiCol.WindowBg) | 0xFF000000u);
            this.arena.DrawBoundary();
        }

        if (pc != null)
        {
            this.arena.ActorMarker(pc.Position, pc.Rotation, pc.HitboxRadius, Colors.PC);
            this.DrawDodge(pc);
        }
    }

    // auto-dodge guidance: mark the safe spot and an arrow to it when the player must move
    private void DrawDodge(Actor pc)
    {
        if (!this.config.AutoDodgeGuidance || !this.ai.HasSolution)
            return;
        var s = this.ai.Current;
        if (!s.NeedToMove)
            return;

        if (s.Found)
        {
            this.arena.AddLine(pc.Position, s.Target, Colors.Safe, 3f);
            this.arena.AddCircleFilled(s.Target, 0.8f, Colors.Safe);
            this.arena.AddCircle(s.Target, 0.8f, Colors.PC, 2f);
        }
        else
        {
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "No safe spot!");
        }
    }

    public void Dispose()
    {
    }
}
