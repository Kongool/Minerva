using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Minerva;
using Minerva.Radar;

namespace Minerva.Windows;

/// <summary>
/// A game-free radar sandbox for testing the renderer and the auto-dodge solver without a duty.
/// Draws every AOE shape type on a synthetic arena, lets you click to move the player, and runs the
/// real <see cref="ArenaPathfinder"/> live so you can watch the dodge target react as you step into
/// and out of danger. This exercises the exact <see cref="ImGuiArena"/> + <see cref="AIHints"/> code
/// the in-game radar uses.
/// </summary>
public sealed class DebugRadarWindow : Window, IDisposable
{
    private sealed class TestAOE(string label, AOEShape shape, WDir offset, bool spin = false)
    {
        public readonly string Label = label;
        public readonly AOEShape Shape = shape;
        public readonly WDir Offset = offset;
        public readonly bool Spin = spin;
        public bool Enabled = true;
    }

    private readonly ImGuiArena arena = new();
    private readonly WPos center = new(100f, 100f);
    private WPos player = new(100f, 88f);
    private float time;
    private int boundsKind; // 0 = square, 1 = circle, 2 = rect
    private bool autoDodge = true;

    private readonly List<TestAOE> aoes =
    [
        new("Circle r5", new AOEShapeCircle(5f), new WDir(-9f, -9f)),
        new("Cone r12 60° (spins)", new AOEShapeCone(12f, 30f.Degrees()), default, spin: true),
        new("Rect 12×6", new AOEShapeRect(12f, 3f), new WDir(9f, 9f)),
        new("Donut 4–9", new AOEShapeDonut(4f, 9f), new WDir(-9f, 9f)),
        new("Cross 8/2", new AOEShapeCross(8f, 2f), new WDir(9f, -9f)),
    ];

    public DebugRadarWindow()
        : base("Minerva Radar Sandbox###MinervaSandbox")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    private ArenaBounds MakeBounds() => this.boundsKind switch
    {
        1 => new ArenaBoundsCircle(20f),
        2 => new ArenaBoundsRect(20f, 14f),
        _ => new ArenaBoundsSquare(20f),
    };

    public override void Draw()
    {
        this.time += ImGui.GetIO().DeltaTime;

        ImGui.TextDisabled("Click inside the arena to move the player. This uses the real renderer + dodge solver — no game needed.");

        ImGui.SetNextItemWidth(140f);
        ImGui.Combo("Arena", ref this.boundsKind, "Square\0Circle\0Rect\0");
        ImGui.SameLine();
        ImGui.Checkbox("Auto-dodge", ref this.autoDodge);
        ImGui.SameLine();
        if (ImGui.Button("Reset player"))
            this.player = new WPos(100f, 88f);

        for (var i = 0; i < this.aoes.Count; ++i)
        {
            if (i > 0)
                ImGui.SameLine();
            ImGui.Checkbox(this.aoes[i].Label, ref this.aoes[i].Enabled);
        }

        // build the frame's danger set
        var bounds = this.MakeBounds();
        var spin = (this.time * 40f).Degrees();
        var hints = new AIHints { Center = this.center, Bounds = bounds, PlayerPosition = this.player };
        var active = new List<(AOEShape shape, WPos origin, Angle rot)>();
        foreach (var a in this.aoes)
        {
            if (!a.Enabled)
                continue;
            var origin = this.center + a.Offset;
            var rot = a.Spin ? spin : default;
            active.Add((a.Shape, origin, rot));
            hints.AddForbiddenZone(a.Shape, origin, rot, DateTime.UtcNow); // already active
        }

        var now = DateTime.UtcNow;
        var solve = ArenaPathfinder.Solve(hints, now, horizonSeconds: 1f);

        // canvas
        var topLeft = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var side = MathF.Max(64f, MathF.Min(avail.X, avail.Y - 4f));
        var size = new Vector2(side, side);
        ImGui.InvisibleButton("##sandbox", size);

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            this.arena.Center = this.center;
            this.arena.Bounds = bounds;
            this.arena.Begin(topLeft, size);
            this.player = this.arena.ScreenToWorld(ImGui.GetMousePos());
        }

        this.arena.Center = this.center;
        this.arena.Bounds = bounds;
        this.arena.Begin(topLeft, size);

        // danger zones
        foreach (var (shape, origin, rot) in active)
            this.arena.ZoneShape(shape, origin, rot, Colors.AOE);

        this.arena.DrawBoundary();

        // a "boss" at center + the player
        this.arena.ActorMarker(this.center, spin, 3f, Colors.Enemy);
        var inDanger = hints.InImminentDanger(this.player, now.AddSeconds(1));
        this.arena.ActorMarker(this.player, default, 0.5f, inDanger ? Colors.Danger : Colors.PC);

        // dodge guidance
        if (this.autoDodge && solve.NeedToMove && solve.Found)
        {
            this.arena.AddLine(this.player, solve.Target, Colors.Safe, 3f);
            this.arena.AddCircleFilled(solve.Target, 0.8f, Colors.Safe);
            this.arena.AddCircle(solve.Target, 0.8f, Colors.PC, 2f);
        }

        // status line
        var status = !solve.NeedToMove ? "SAFE — hold position"
            : solve.Found ? $"DODGE to [{solve.Target.X:f0}, {solve.Target.Z:f0}]"
            : "NO SAFE SPOT";
        ImGui.TextColored(inDanger ? new Vector4(1f, 0.4f, 0.4f, 1f) : new Vector4(0.5f, 1f, 0.6f, 1f),
            $"Player [{this.player.X:f0}, {this.player.Z:f0}]  —  {status}");
    }

    public void Dispose()
    {
    }
}
