using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Minerva.Automation;
using Minerva.GameSync;
using Minerva.Modules;
using Minerva.Radar;

namespace Minerva.Windows;

/// <summary>
/// The arena view itself — the field, the party, the dodge marker — with the fight's readout drawn over it:
/// who and where at the top left, the countdown at the top right, the hints along the bottom. No window
/// chrome of its own; <see cref="ArenaOverlayWindow"/> supplies that and the toolbar under the field.
/// </summary>
public sealed class RadarView(ModuleManager manager, AIManager ai, Configuration config)
{
    private readonly ModuleManager manager = manager;
    private readonly AIManager ai = ai;
    private readonly Configuration config = config;
    private readonly ImGuiArena arena = new();

    /// <summary>
    /// The window background at a given opacity, for both the field fill and the overspill mask.
    /// <para>Taken from the live theme rather than a constant so the radar keeps matching the rest of the
    /// UI if the theme changes.</para>
    /// </summary>
    public static Vector4 BackgroundVec(float opacity)
    {
        var bg = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.WindowBg));
        bg.W = Math.Clamp(opacity, 0f, 1f);
        return bg;
    }

    /// <summary>The same colour packed for the draw list.</summary>
    public static uint BackgroundColor(float opacity) => ImGui.ColorConvertFloat4ToU32(BackgroundVec(opacity));

    /// <summary>True when a module is active, i.e. there is actually a fight to draw.</summary>
    public bool HasEncounter => this.manager.ActiveModule != null;

    /// <param name="reserveBottom">Height to leave under the field for the caller's own row.</param>
    public void Draw(float reserveBottom = 0f)
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

        // arena canvas
        var canvasTopLeft = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var side = MathF.Min(avail.X, avail.Y - reserveBottom);
        if (side < 32f)
            return;
        var canvasSize = new Vector2(side, side);
        ImGui.InvisibleButton("##canvas", canvasSize); // reserve the region

        this.arena.Center = module.Center;
        this.arena.Bounds = module.Bounds;
        this.arena.ClipZones = this.config.ClipToArena;
        // camera-align: put the camera's forward direction at the top of the screen. W2S rotates the world
        // offset by Rotation, so a world direction θ ends up at the top when Rotation = θ - π; the camera's
        // forward is (azimuth + π) in world-rotation terms, which cancels down to just the azimuth.
        this.arena.Rotation = this.config.RadarHeading == RadarHeading.CameraAlign && GameData.TryCameraAzimuth(out var azimuth)
            ? azimuth
            : 0f;
        this.arena.Begin(canvasTopLeft, canvasSize);

        module.Arena = this.arena;

        // draw the local player on top of module content via the foreground pass; module draws the rest
        module.DrawArena(pcSlot, pc ?? module.PrimaryActor);

        // confine danger zones to the field: mask everything past the boundary, then restroke the border
        if (this.config.ClipToArena)
        {
            this.arena.ClipOutsideArena(BackgroundColor(this.config.RadarOpacity));
            this.arena.DrawBoundary();
        }

        this.arena.DrawCompass();

        if (this.config.ShowPartyMembers)
            this.DrawParty(module, pc);

        var noSafeSpot = false;
        if (pc != null)
        {
            this.arena.ActorMarker(pc.Position, pc.Rotation, pc.HitboxRadius, Colors.PC);
            noSafeSpot = this.DrawDodge(pc);
        }

        this.arena.End();

        this.DrawHud(module, pc, pcSlot, canvasTopLeft, canvasSize, noSafeSpot);
    }

    /// <summary>
    /// The readout, over the field rather than above it: a line of text on the arena costs no height, and
    /// what you look at during a fight should be one thing, not a stack of things.
    /// </summary>
    private void DrawHud(ModuleBase module, Actor? pc, int pcSlot, Vector2 topLeft, Vector2 size, bool noSafeSpot)
    {
        var dl = ImGui.GetWindowDrawList();
        var pad = new Vector2(8f, 6f);
        var line = ImGui.GetTextLineHeightWithSpacing();

        // top-left: who, and where in the fight
        var name = this.manager.ActiveModuleInfo is { } info ? ModulesTab.BossName(info) : module.GetType().Name;
        UiKit.Shadowed(dl, topLeft + pad, UiKit.U32(AegisTheme.Travertine), name.ToUpperInvariant());

        var sm = module.StateMachine;
        var where = sm.ActivePhaseIndex >= 0 ? $"Phase {sm.ActivePhaseIndex + 1}" : "Pre-pull";
        if (sm.ActiveState is { } state)
            where += $" · {state.Name}";
        UiKit.Shadowed(dl, topLeft + pad + new Vector2(0f, line), UiKit.U32(AegisTheme.TravertineDim), where);

        // top-right: the countdown to the end of the current state. An estimate from the module author's
        // declared durations, exactly as good as their timings -- which is also what BossmodReborn shows.
        // an hour-plus "enrage" is TrivialPhase's placeholder duration, not a timer anyone wants on screen
        if (sm.ActiveState is { Duration: > 0f and < 3600f } current)
        {
            var remaining = current.Duration - (module.World.CurrentTime - sm.LastTransition).TotalSeconds;
            if (remaining >= 0d)
            {
                var text = remaining.ToString("0.0", CultureInfo.InvariantCulture);
                ImGui.SetWindowFontScale(1.8f);
                var width = ImGui.CalcTextSize(text).X;
                var color = remaining < 2d ? UiKit.Red : AegisTheme.BronzeBright;
                UiKit.Shadowed(dl, new Vector2(topLeft.X + size.X - pad.X - width, topLeft.Y + pad.Y - 2f), UiKit.U32(color), text);
                ImGui.SetWindowFontScale(1f);
            }
        }

        // bottom-left: the hints, then the auto-move readout
        var lines = new List<(string Text, Vector4 Color)>();
        var global = new ModuleComponent.GlobalHints();
        module.AddGlobalHints(global);
        foreach (var h in global)
            lines.Add((h, UiKit.Yellow));

        if (pc != null)
        {
            var hints = new ModuleComponent.TextHints();
            module.AddHints(pcSlot, pc, hints);
            foreach (var (text, risk) in hints)
                lines.Add((text, risk ? UiKit.Red : UiKit.Grey));
        }

        if (noSafeSpot)
            lines.Add(("No safe spot!", UiKit.Red));

        // auto-move diagnostics: makes it obvious why steering may not be happening
        if (this.config.AutoDodgeEnabled && this.ai.Movement is MovementController mc)
        {
            var backend = mc.NavmeshBackend ?? "direct";
            if (mc.Steering)
                lines.Add(($"Auto-move: steering to safe spot ({backend})", UiKit.Green));
            else if (!mc.UsingNavmesh && !mc.HookInstalled)
                lines.Add(("Auto-move: no navmesh and movement hook NOT installed (signature outdated) — check /xllog", UiKit.Red));
            else if (this.ai.HasSolution && this.ai.Current.NeedToMove && !this.ai.Current.Found)
                lines.Add(("Auto-move: in danger but no safe spot found", UiKit.Amber));
            else
                lines.Add(($"Auto-move: idle ({backend}, no imminent danger)", AegisTheme.TravertineDim));
        }

        var y = topLeft.Y + size.Y - pad.Y - (line * lines.Count);
        foreach (var (text, color) in lines)
        {
            UiKit.Shadowed(dl, new Vector2(topLeft.X + pad.X, y), UiKit.U32(color), text);
            y += line;
        }
    }

    /// <summary>
    /// Draw the rest of the party, coloured by whatever <see cref="Configuration.PartyColorBy"/> asks for.
    /// <para>Drawn before the local player so you are always on top of the pile — in a stack mechanic
    /// everyone occupies the same yard, and the one marker you must be able to find is your own.</para>
    /// </summary>
    private void DrawParty(ModuleBase module, Actor? self)
    {
        var party = module.Raid;
        for (var slot = 0; slot < PartyState.MaxSlots; ++slot)
        {
            var member = party[slot];
            if (member == null || member.IsDeadOrDestroyed || member == self)
                continue;

            var color = this.config.PartyColorBy switch
            {
                PartyColoring.Role => Colors.ForRole(member.Role),
                // Light parties are the game's own split: first four slots and last four. Anything more
                // elaborate would need a role assignment that may not exist.
                PartyColoring.Group => Colors.ForGroup(slot < PartyState.MaxSlots / 2 ? 0 : 1),
                _ => Colors.Player,
            };
            this.arena.ActorMarker(member.Position, member.Rotation, MathF.Max(member.HitboxRadius, 0.5f), color);
        }
    }

    // auto-dodge guidance: mark the safe spot and an arrow to it when the player must move.
    // Returns true when the player must move and there is nowhere to go, for the readout.
    private bool DrawDodge(Actor pc)
    {
        if (!this.config.AutoDodgeGuidance || !this.ai.HasSolution)
            return false;
        var s = this.ai.Current;
        if (!s.NeedToMove)
            return false;

        if (!s.Found)
            return true;

        this.arena.AddLine(pc.Position, s.Target, Colors.Safe, 3f);
        this.arena.AddCircleFilled(s.Target, 0.8f, Colors.Safe);
        this.arena.AddCircle(s.Target, 0.8f, Colors.PC, 2f);
        return false;
    }
}
