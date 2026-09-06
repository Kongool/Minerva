using System;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Minerva.Replay;

namespace Minerva.Windows;

/// <summary>
/// The Record page: the recorder and the saved recordings on the left, a report on the right — fact sheet,
/// validation or draft preview — with the playback under it. Record a fight, stop to get a fact sheet and a
/// draft module, watch it back with the matching module active, and see how well that module covers it.
/// </summary>
public sealed class ReplayTab
{
    private static readonly string[] Views = ["Fact sheet", "Validation", "Preview"];
    private static readonly Vector4 ReportGreen = new(0.5f, 1f, 0.6f, 1f);
    private const float RecorderWidth = 270f;

    private readonly ReplayService replay;
    private string view = "Fact sheet";
    private string? generationReport;
    private string? validationReport;
    private string? previewReport;

    public ReplayTab(ReplayService replay) => this.replay = replay;

    public void Draw()
    {
        if (ImGui.BeginChild("##recorder", new Vector2(RecorderWidth, 0f), false))
            this.DrawRecorder();
        ImGui.EndChild();
        ImGui.SameLine();
        if (ImGui.BeginChild("##report", new Vector2(0f, 0f), false))
            this.DrawReport();
        ImGui.EndChild();
    }

    private void DrawRecorder()
    {
        var recording = this.replay.IsRecording;
        var cfg = this.replay.Config;
        UiKit.BeginCard("Recorder", recording ? "recording" : "idle");

        if (recording)
            ImGui.TextColored(UiKit.Red, "● Recording…");
        else
            ImGui.TextDisabled("Idle.");

        if (UiKit.Toggle(recording ? "Stop & analyze" : "Start recording", recording, UiKit.Alpha(UiKit.Red, 0.18f), UiKit.Alpha(UiKit.Red, 0.7f)))
            this.replay.Toggle();
        ImGui.SameLine();
        if (ImGui.Button("Open folder"))
            this.replay.OpenFolder();

        var exclude = cfg.RecordExcludeOtherPlayers;
        if (ImGui.Checkbox("Ignore other players", ref exclude))
        {
            cfg.RecordExcludeOtherPlayers = exclude;
            cfg.Save();
        }
        UiKit.Tip("Smaller, cleaner recordings. Applies to the next recording.");

        // Next to the recorder it arms, so the one setting that can start a recording by itself is visible
        // wherever recordings are looked at.
        var auto = cfg.AutoRecordEncounters;
        if (ImGui.Checkbox("Auto-record boss encounters", ref auto))
        {
            cfg.AutoRecordEncounters = auto;
            cfg.Save();
        }
        UiKit.Tip("Starts by itself when a boss is engaged, module or not, and stops when the fight ends.");

        ImGui.Spacing();
        UiKit.Eyebrow("Recordings");
        var listHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - 12f;
        if (ImGui.BeginChild("##recordings", new Vector2(0f, MathF.Max(listHeight, 60f)), true))
        {
            var current = this.replay.PlaybackPath ?? string.Empty;
            var recordings = this.replay.ListRecordings();
            if (recordings.Count == 0)
                ImGui.TextDisabled("None yet.");
            foreach (var path in recordings)
            {
                if (ImGui.Selectable($"{Display(path)}##{path}", path == current))
                    this.Load(path);
            }
        }
        ImGui.EndChild();

        if (this.replay.PlaybackPath != null && ImGui.Button("Reveal"))
            this.replay.RevealFile(this.replay.PlaybackPath);
        UiKit.Tip("Show this recording in Explorer.");

        UiKit.EndCard(fill: true);
    }

    private void DrawReport()
    {
        var player = this.replay.Player;
        UiKit.BeginCard("Report", this.replay.PlaybackPath != null ? Display(this.replay.PlaybackPath) : "no recording loaded");

        for (var i = 0; i < Views.Length; ++i)
        {
            if (i != 0)
                ImGui.SameLine(0f, 0f);
            if (UiKit.Seg(Views[i], this.view == Views[i]))
                this.view = Views[i];
        }

        // the report itself, in a scrolling box that leaves room for the playback under it
        var avail = ImGui.GetContentRegionAvail();
        var reportHeight = player != null ? MathF.Max(120f, avail.Y * 0.4f) : MathF.Max(120f, avail.Y - 60f);
        if (ImGui.BeginChild("##view", new Vector2(0f, reportHeight), true))
        {
            switch (this.view)
            {
                case "Validation": this.DrawValidation(player); break;
                case "Preview": this.DrawPreview(player); break;
                default: this.DrawFactSheet(); break;
            }
        }
        ImGui.EndChild();

        this.DrawActions(player);

        if (player != null)
        {
            DrawTransport(player);
            DrawDecision(player);
            DrawArenaCanvas(player);
        }
        else
        {
            ImGui.TextWrapped("No replay loaded. Record a fight — it loads here when you stop — or pick one from the list.");
        }

        UiKit.EndCard(fill: true);
    }

    private void DrawFactSheet()
    {
        var sheet = this.replay.LastFactSheet;
        if (string.IsNullOrEmpty(sheet))
        {
            ImGui.TextWrapped("Record a full boss rotation, then stop to generate a fact sheet (object ids, action ids with " +
                "cast times, statuses, tethers, icons, arena center). Re-analyze rebuilds it from the loaded recording.");
            return;
        }

        if (!string.IsNullOrEmpty(this.generationReport))
            ImGui.TextColored(ReportGreen, this.generationReport);
        ImGui.TextDisabled(this.replay.LastPath ?? string.Empty);
        ImGui.TextUnformatted(sheet);
    }

    private void DrawValidation(ReplayPlayer? player)
    {
        if (player == null)
        {
            ImGui.TextWrapped("Load or record a fight, then Run validation to see how well its module (compiled into the " +
                "plugin) covers the mechanics — and watch it play below.");
            return;
        }

        if (string.IsNullOrEmpty(this.validationReport))
        {
            ImGui.TextDisabled($"module: {player.ModuleName}");
            ImGui.TextWrapped("Run validation to replay the log through the module and list which enemy casts it draws, hints, or ignores.");
            return;
        }

        DrawValidationReport(this.validationReport);
    }

    // Preview draft: draw the would-be module's AOEs (each cast's classified shape) without compiling
    // anything — so you can eyeball a generated draft in-game before adding it to the project.
    private void DrawPreview(ReplayPlayer? player)
    {
        if (player == null)
        {
            ImGui.TextWrapped("Load or record a fight, then Build preview to draw its generated draft's AOEs over the " +
                "playback — no compiling needed.");
            return;
        }

        if (!string.IsNullOrEmpty(this.previewReport))
            ImGui.TextColored(ReportGreen, this.previewReport);
        ImGui.TextWrapped("Approximates the draft from game data — no compile: fixed shapes, spread/stack on the marked " +
            "player, and tether AOEs on the tethered target. Phase logic + custom components need the compiled module.");
        if (player.PreviewActive)
            ImGui.TextColored(UiKit.Green, "Preview shapes are drawn on the playback below.");
    }

    private void DrawActions(ReplayPlayer? player)
    {
        switch (this.view)
        {
            case "Validation":
                if (player == null)
                    return;
                if (ImGui.Button("Run validation"))
                    this.validationReport = this.replay.ValidateModule();
                ImGui.SameLine();
                ImGui.TextDisabled($"module: {player.ModuleName}");
                break;

            case "Preview":
                if (player == null)
                    return;
                if (ImGui.Button("Build preview from draft"))
                    this.previewReport = this.replay.BuildPreview();
                if (player.PreviewActive)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Clear preview"))
                        player.ClearPreview();
                }
                break;

            default:
                var sheet = this.replay.LastFactSheet;
                if (!string.IsNullOrEmpty(sheet))
                {
                    if (ImGui.Button("Copy fact sheet"))
                        ImGui.SetClipboardText(sheet);
                    if (this.replay.LastInput != null)
                    {
                        ImGui.SameLine();
                        if (ImGui.Button("Generate module"))
                            this.generationReport = this.replay.GenerateModule();
                    }
                    ImGui.SameLine();
                }
                if (this.replay.PlaybackPath != null && ImGui.Button("Re-analyze & regenerate"))
                    this.generationReport = this.replay.RegenerateFromPlayback();
                if (this.replay.LastGeneratedPath != null)
                {
                    if (ImGui.Button("Copy generated path"))
                        ImGui.SetClipboardText(this.replay.LastGeneratedPath);
                    ImGui.SameLine();
                    if (ImGui.Button("Reveal generated file"))
                        this.replay.RevealFile(this.replay.LastGeneratedPath);
                }
                break;
        }
    }

    // --- shared helpers ---

    // "minerva-20260903-214210-Dead Stars.log" -> "20260903-214210-Dead Stars": the prefix is the same on every
    // row, the extension too, and the boss name is what you are looking for
    private static string Display(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.StartsWith("minerva-", StringComparison.OrdinalIgnoreCase) ? name[8..] : name;
    }

    private void Load(string path)
    {
        try
        {
            this.replay.LoadPlayback(path);
            this.validationReport = null;
            this.previewReport = null;
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, $"Minerva: could not load recording {path}.");
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
        ImGui.SetNextItemWidth(120f);
        ImGui.SliderFloat("##speed", ref player.Speed, 0.1f, 4f, "%.1fx");
        ImGui.SameLine();
        ImGui.TextDisabled($"{player.ModuleName} · {player.OpCount} ops");

        var progress = player.Progress;
        ImGui.SetNextItemWidth(-float.Epsilon);
        if (ImGui.SliderFloat("##timeline", ref progress, 0f, 1f, $"{player.PositionSeconds:0.0} / {player.DurationSeconds:0.0}s"))
            player.Seek(progress);
    }

    // what the dodge was doing at the cursor, from the recording -- the "why" behind the line on the canvas.
    // Older recordings carry no decisions, and then nothing is shown rather than a guess.
    private static void DrawDecision(ReplayPlayer player)
    {
        var d = player.Decision;
        if (!d.Known)
            return;
        var dist = d.Found && player.PlayerPosition is { } me ? (d.Target - me).Length() : 0f;
        var text = "Dodge: " + d.Describe(dist);
        if (!d.NeedToMove)
            ImGui.TextDisabled(text);
        else
            ImGui.TextColored(d.Steering ? UiKit.Green : UiKit.Amber, text);
    }

    private static void DrawArenaCanvas(ReplayPlayer player)
    {
        var topLeft = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail();
        var side = MathF.Min(avail.X, avail.Y - 10f);
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
            var color = line.Contains("UNCOVERED") || line.Contains("⚠") ? new Vector4(1f, 0.8f, 0.2f, 1f) // amber: needs attention
                : line.TrimStart().StartsWith("drawn") ? ReportGreen   // green: covered
                : new Vector4(0.85f, 0.85f, 0.85f, 1f);
            ImGui.TextColored(color, line);
        }
    }
}
