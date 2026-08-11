using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Minerva;

namespace Minerva.Windows;

/// <summary>
/// Inspector that renders the mirrored <see cref="WorldState"/> — proof that the Phase-2
/// game-sync reflects the live game. Shows frame/zone info, a live actor table, and a rolling
/// log of transient packet events (icons, VFX, tethers, map effects, director updates, RSV)
/// captured by the Phase-2b hooks.
/// </summary>
public sealed class WorldStateDebugWindow : Window, IDisposable
{
    private const int MaxEvents = 200;

    private readonly WorldState world;
    private readonly Queue<string> events = new();
    private readonly List<EventSubscription> subs = [];
    private bool enemiesOnly;
    private bool autoScroll = true;

    public WorldStateDebugWindow(WorldState world)
        : base("Minerva — World State###MinervaWorldState")
    {
        this.world = world;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 380),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        // subscribe to the transient events so we can prove the hooks fire in-game
        this.subs.Add(world.Actors.IconAppeared.Subscribe((a, e) => this.Log($"ICON {e.IconID} on {Short(a)} -> {e.TargetID:X}")));
        this.subs.Add(world.Actors.VFXAppeared.Subscribe((a, e) => this.Log($"VFX  {e.VFXID} on {Short(a)}")));
        this.subs.Add(world.Actors.Tethered.Subscribe(a => this.Log($"TETH {a.Tether.ID} {Short(a)} -> {a.Tether.Target:X}")));
        this.subs.Add(world.Actors.Untethered.Subscribe(a => this.Log($"TETH- {Short(a)}")));
        this.subs.Add(world.MapEffect.Subscribe(op => this.Log($"ENVC idx {op.Index:X2} state {op.State:X8}")));
        this.subs.Add(world.DirectorUpdate.Subscribe(op => this.Log($"DIRU {op.UpdateID:X8} {op.Param1:X8} {op.Param2:X8}")));
        this.subs.Add(world.RSVDataReceived.Subscribe(op => this.Log($"RSV  {op.Key} = {op.Value}")));
    }

    private static string Short(Actor a) => $"{a.Name}<{a.OID:X}>";

    private void Log(string line)
    {
        this.events.Enqueue($"[{this.world.Frame.Index}] {line}");
        while (this.events.Count > MaxEvents)
            this.events.Dequeue();
    }

    public override void Draw() => this.DrawContent();

    /// <summary>The inspector body, factored out so it can also be embedded as a tab in the radar window.</summary>
    public void DrawContent()
    {
        var w = this.world;
        ImGui.TextUnformatted($"Frame {w.Frame.Index}  |  dt {w.Frame.Duration * 1000f:f1} ms  |  zone {w.CurrentZone}  cfc {w.CurrentCFCID}  |  actors {w.Actors.Actors.Count}  |  rsv {w.RSVEntries.Count}");
        ImGui.Separator();

        if (ImGui.BeginTabBar("tabs"))
        {
            if (ImGui.BeginTabItem("Actors"))
            {
                this.DrawActors();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem($"Events ({this.events.Count})"))
            {
                this.DrawEvents();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawActors()
    {
        ImGui.Checkbox("Enemies only", ref this.enemiesOnly);

        if (!ImGui.BeginTable("actors", 7, ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders))
            return;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("OID", ImGuiTableColumnFlags.WidthFixed, 64f);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 72f);
        ImGui.TableSetupColumn("Pos", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("HP%", ImGuiTableColumnFlags.WidthFixed, 52f);
        ImGui.TableSetupColumn("Flags", ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableSetupColumn("Cast", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var a in this.world.Actors.Actors.Values)
        {
            if (this.enemiesOnly && a.Type != ActorType.Enemy)
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted(a.Name.Length > 0 ? a.Name : "<unnamed>");
            ImGui.TableNextColumn(); ImGui.TextUnformatted($"{a.OID:X}");
            ImGui.TableNextColumn(); ImGui.TextUnformatted(a.Type.ToString());
            ImGui.TableNextColumn(); ImGui.TextUnformatted($"{a.Position.X:f1}, {a.Position.Z:f1}");
            ImGui.TableNextColumn(); ImGui.TextUnformatted(a.HPMP.MaxHP > 0 ? $"{a.HPRatio * 100f:f0}" : "-");

            var flags = "";
            if (a.InCombat) flags += "C";
            if (!a.IsTargetable) flags += "u";
            if (a.IsDead) flags += "D";
            if (a.IsAlly) flags += "a";
            if (a.Tether.Target != 0) flags += "T";
            ImGui.TableNextColumn(); ImGui.TextUnformatted(flags.Length > 0 ? flags : "-");

            ImGui.TableNextColumn();
            if (a.CastInfo is { } c)
                ImGui.TextUnformatted($"{c.Action.ID} {c.ElapsedTime:f1}/{c.TotalTime:f1}s @ {c.LocXZ.X:f0},{c.LocXZ.Z:f0}");
            else
                ImGui.TextUnformatted("-");
        }

        ImGui.EndTable();
    }

    private void DrawEvents()
    {
        ImGui.Checkbox("Auto-scroll", ref this.autoScroll);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            this.events.Clear();

        if (!ImGui.BeginChild("eventlog", new Vector2(0, 0), true))
        {
            ImGui.EndChild();
            return;
        }

        foreach (var line in this.events)
            ImGui.TextUnformatted(line);

        if (this.autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 1f)
            ImGui.SetScrollHereY(1f);

        ImGui.EndChild();
    }

    public void Dispose()
    {
        foreach (var s in this.subs)
            s.Dispose();
        this.subs.Clear();
    }
}
