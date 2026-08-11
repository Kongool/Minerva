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

        if (ImGui.BeginTabItem("Radar"))
        {
            this.DrawRadar();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Menu"))
        {
            this.menu.DrawContent();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Replay"))
        {
            this.replay.DrawContent();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Debug"))
        {
            this.debug.DrawContent();
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
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
        this.arena.Begin(canvasTopLeft, canvasSize);

        module.Arena = this.arena;

        // draw the local player on top of module content via the foreground pass; module draws the rest
        module.DrawArena(pcSlot, pc ?? module.PrimaryActor);
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
