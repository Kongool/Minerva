using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Minerva.Replay;

namespace Minerva.Windows;

/// <summary>
/// Recording, playback, and module-validation UI. Record a fight (fact sheet + generation on stop),
/// watch it back with the matching module active, and validate how well that module covers the fight.
/// </summary>
public sealed class ReplayWindow : Window, IDisposable
{
    private readonly ReplayService replay;
    private string? generationReport;
    private string? validationReport;

    public ReplayWindow(ReplayService replay)
        : base("Minerva Replay###MinervaReplay")
    {
        this.replay = replay;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 380),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw() => this.DrawContent();

    /// <summary>The replay body, factored out so it can also be embedded as a tab in the radar window.</summary>
    public void DrawContent()
    {
        if (!ImGui.BeginTabBar("###replaytabs"))
            return;

        if (ImGui.BeginTabItem("Record & analyze"))
        {
            this.DrawRecord();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Playback"))
        {
            this.DrawPlayback();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Validate"))
        {
            this.DrawValidate();
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
    }

    private void DrawRecord()
    {
        var recording = this.replay.IsRecording;
        if (recording)
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "● Recording…");
        else
            ImGui.TextDisabled("Idle.");

        if (ImGui.Button(recording ? "Stop & analyze" : "Start recording"))
            this.replay.Toggle();
        ImGui.SameLine();
        if (ImGui.Button("Open folder"))
            this.replay.OpenFolder();

        var exclude = this.replay.Config.RecordExcludeOtherPlayers;
        if (ImGui.Checkbox("Ignore other players (smaller, cleaner recordings)", ref exclude))
        {
            this.replay.Config.RecordExcludeOtherPlayers = exclude;
            this.replay.Config.Save();
        }
        if (recording)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(applies to the next recording)");
        }

        var sheet = this.replay.LastFactSheet;
        if (!string.IsNullOrEmpty(sheet))
        {
            ImGui.SameLine();
            if (ImGui.Button("Copy fact sheet"))
                ImGui.SetClipboardText(sheet);

            if (this.replay.LastInput != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Generate module"))
                    this.generationReport = this.replay.GenerateModule();
            }

            if (!string.IsNullOrEmpty(this.generationReport))
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.6f, 1f), this.generationReport);
                if (this.replay.LastGeneratedPath != null)
                {
                    if (ImGui.Button("Copy generated path"))
                        ImGui.SetClipboardText(this.replay.LastGeneratedPath);
                    ImGui.SameLine();
                    if (ImGui.Button("Reveal generated file"))
                        this.replay.RevealFile(this.replay.LastGeneratedPath);
                }
            }

            ImGui.Separator();
            ImGui.TextDisabled(this.replay.LastPath ?? "");
            if (ImGui.BeginChild("factsheet", new Vector2(0, 0), true))
            {
                ImGui.TextUnformatted(sheet);
                ImGui.EndChild();
            }
        }
        else
        {
            ImGui.Separator();
            ImGui.TextWrapped("Record a full boss rotation, then stop to generate a fact sheet " +
                "(object ids, action ids with cast times, statuses, tethers, icons, arena center).");
        }
    }

    private void DrawPlayback()
    {
        this.DrawRecordingsPicker();

        var player = this.replay.Player;
        if (player == null)
        {
            ImGui.TextWrapped("No replay loaded. Record a fight — it loads here automatically when you stop — " +
                "then use Play and the timeline to watch it back.");
            return;
        }

        DrawTransport(player);
        ImGui.TextDisabled($"{player.ModuleName}   ·   {player.OpCount} ops");
        if (ImGui.Button("Re-analyze & regenerate module"))
            this.generationReport = this.replay.RegenerateFromPlayback();
        if (!string.IsNullOrEmpty(this.generationReport))
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.6f, 1f), this.generationReport);

        DrawArenaCanvas(player);
    }

    // Validate: watch the compiled module play the recording (AOEs + green safe-spot) alongside a
    // coverage report of which enemy casts it draws / hints / ignores.
    private void DrawValidate()
    {
        this.DrawRecordingsPicker();

        var player = this.replay.Player;
        if (player == null)
        {
            ImGui.TextWrapped("No replay loaded. Load or record a fight, then Run validation to see how well " +
                "its module (compiled into the plugin) covers the mechanics — and watch it play below.");
            return;
        }

        if (ImGui.Button("Run validation"))
            this.validationReport = this.replay.ValidateModule();
        ImGui.SameLine();
        ImGui.TextDisabled($"module: {player.ModuleName}");

        if (!string.IsNullOrEmpty(this.validationReport))
            DrawValidationReport(this.validationReport);

        ImGui.Separator();
        DrawTransport(player);
        DrawArenaCanvas(player);
    }

    // --- shared helpers ---

    private void DrawRecordingsPicker()
    {
        if (ImGui.Button("Open folder"))
            this.replay.OpenFolder();
        if (this.replay.PlaybackPath != null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Reveal this recording"))
                this.replay.RevealFile(this.replay.PlaybackPath);
        }

        var recordings = this.replay.ListRecordings();
        if (recordings.Count == 0)
            return;

        ImGui.TextDisabled("Recording:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);
        var current = this.replay.PlaybackPath ?? "";
        if (ImGui.BeginCombo("##recordings", System.IO.Path.GetFileName(current)))
        {
            foreach (var path in recordings)
                if (ImGui.Selectable(System.IO.Path.GetFileName(path), path == current))
                    this.replay.LoadPlayback(path);
            ImGui.EndCombo();
        }
    }

    private static void DrawTransport(ReplayPlayer player)
    {
        var playLabel = player.Playing ? "Pause" : player.AtEnd ? "Replay" : "Play";
        if (ImGui.Button(playLabel))
            player.TogglePlay();
        ImGui.SameLine();
        if (ImGui.Button("Restart"))
            player.Restart();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140f);
        ImGui.SliderFloat("Speed", ref player.Speed, 0.1f, 4f, "%.1fx");

        var progress = player.Progress;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.SliderFloat("##timeline", ref progress, 0f, 1f, $"{player.PositionSeconds:0.0} / {player.DurationSeconds:0.0}s"))
            player.Seek(progress);
    }

    private static void DrawArenaCanvas(ReplayPlayer player)
    {
        var topLeft = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var side = MathF.Min(avail.X, avail.Y);
        if (side < 32f)
            return;
        var size = new Vector2(side, side);
        ImGui.InvisibleButton("##replaycanvas", size);
        player.DrawArena(topLeft, size);
    }

    private static void DrawValidationReport(string report)
    {
        foreach (var raw in report.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0)
                continue;
            var color = line.Contains("UNCOVERED") ? new Vector4(1f, 0.8f, 0.2f, 1f)      // amber: needs attention
                : line.TrimStart().StartsWith("drawn") ? new Vector4(0.5f, 1f, 0.6f, 1f)   // green: covered
                : new Vector4(0.85f, 0.85f, 0.85f, 1f);
            ImGui.TextColored(color, line);
        }
    }

    public void Dispose()
    {
    }
}
