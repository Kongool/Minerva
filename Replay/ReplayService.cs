using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Dalamud.Game.DutyState;
using Minerva;
using Minerva.Generation;
using Minerva.Modules;

namespace Minerva.Replay;

/// <summary>
/// Plugin-side replay control: records the live <see cref="WorldState"/> to a timestamped log in
/// the plugin config dir, and analyses a saved log into a fact sheet. Recording just wraps
/// <see cref="ReplayRecorder"/> over a file; analysis replays the file offline through
/// <see cref="ReplayParser"/> and renders <see cref="ReplayAnalysis"/>.
/// </summary>
public sealed class ReplayService : IDisposable
{
    private static readonly TimeSpan EncounterEndGracePeriod = TimeSpan.FromSeconds(2);

    private readonly WorldState world;
    private readonly ModuleManager modules;
    private readonly Configuration config;
    private readonly string directory;
    private readonly RecordingCompletionDetector recordingCompletion = new(EncounterEndGracePeriod);
    private readonly EventSubscription actorDeathSubscription;

    private ReplayRecorder? recorder;
    private StreamWriter? writer;
    private string? currentPath;

    private ModuleRegistry? registry;

    public Configuration Config => this.config;
    public bool IsRecording => this.recorder != null;
    public string? LastFactSheet { get; private set; }
    public string? LastPath { get; private set; }
    public GenerationInput? LastInput { get; private set; }
    public string? LastGeneratedPath { get; private set; }

    /// <summary>The loaded interactive replay, if any (set after a recording stops, or via LoadPlayback).</summary>
    public ReplayPlayer? Player { get; private set; }

    /// <summary>Path of the log currently loaded for playback (used to re-analyze / regenerate).</summary>
    public string? PlaybackPath { get; private set; }

    // built once (reflection scan), reused across playback rebuilds/loads
    private ModuleRegistry Registry => this.registry ??= ModuleRegistry.Build(Assembly.GetExecutingAssembly(), typeof(ModuleRegistry).Assembly);

    public ReplayService(WorldState world, ModuleManager modules, Configuration config)
    {
        this.world = world;
        this.modules = modules;
        this.config = config;
        this.directory = Path.Combine(Service.PluginInterface.ConfigDirectory.FullName, "replays");
        Directory.CreateDirectory(this.directory);
        this.actorDeathSubscription = world.Actors.IsDeadChanged.Subscribe(this.OnActorDeath);
        Service.DutyState.DutyCompleted += this.OnDutyCompleted;
        this.LoadMostRecent(); // so the last recording survives a plugin reload
    }

    // load the newest saved recording for playback (filenames are timestamped, so the last sorts newest)
    private void LoadMostRecent()
    {
        try
        {
            var logs = Directory.GetFiles(this.directory, "minerva-*.log");
            if (logs.Length == 0)
                return;
            Array.Sort(logs);
            this.LoadPlayback(logs[^1]);
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Minerva: could not load the most recent recording.");
        }
    }

    /// <summary>Start recording if idle, otherwise stop and produce a fact sheet.</summary>
    public string Toggle()
    {
        if (this.IsRecording)
        {
            var opCount = this.Stop();
            return $"Stopped. {opCount} ops -> {Path.GetFileName(this.LastPath)}. Fact sheet ready.";
        }
        this.Start();
        return $"Recording to {Path.GetFileName(this.currentPath)}.";
    }

    public void Start()
    {
        if (this.IsRecording)
            return;
        this.currentPath = Path.Combine(this.directory, $"minerva-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        this.writer = new StreamWriter(this.currentPath);
        var localId = Service.ObjectTable[0]?.GameObjectId ?? 0;
        this.recorder = new ReplayRecorder(this.world, this.writer, this.config.RecordExcludeOtherPlayers, localId);
        this.recordingCompletion.Reset();
    }

    public int Stop()
    {
        if (!this.IsRecording)
            return 0;
        var opCount = this.recorder!.OpCount;
        this.recorder.Dispose();
        this.writer!.Dispose();
        this.recorder = null;
        this.writer = null;
        this.recordingCompletion.Reset();
        this.LastPath = this.currentPath;
        Service.Log.Information($"Minerva: recorded {opCount} ops to {this.currentPath}.");

        try
        {
            this.LastFactSheet = this.Analyze(this.currentPath!);
            Service.Log.Information("Minerva fact sheet:\n" + this.LastFactSheet);
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Minerva: failed to analyze recording.");
            this.LastFactSheet = "Analysis failed: " + ex.Message;
        }

        // load the just-recorded fight for interactive playback
        try
        {
            this.LoadPlayback(this.currentPath!);
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Minerva: failed to load recording for playback.");
        }

        return opCount;
    }

    /// <summary>
    /// Stop only after a positive completion signal. Combat loss is intentionally ignored so a wipe,
    /// player death, or temporary disengage cannot end the recording.
    /// </summary>
    public string? UpdateRecording(TimeSpan realDt)
    {
        if (!this.IsRecording || !this.recordingCompletion.Update(realDt))
            return null;

        var opCount = this.Stop();
        return $"Encounter ended. Recording stopped automatically: {opCount} ops -> {Path.GetFileName(this.LastPath)}. Fact sheet ready.";
    }

    private void OnActorDeath(Actor actor)
    {
        var active = this.modules.ActiveModule;
        if (this.IsRecording && actor.IsDead && active != null && ReferenceEquals(actor, active.PrimaryActor)
            && this.modules.ActiveModuleInfo?.Attr.PrimaryActorDeathEndsEncounter == true)
            this.recordingCompletion.SignalCompletion();
    }

    private void OnDutyCompleted(IDutyStateEventArgs args)
    {
        if (this.IsRecording)
            this.recordingCompletion.SignalCompletion();
    }

    /// <summary>Parse a saved log into an interactive <see cref="ReplayPlayer"/> (replaces any current one).</summary>
    public string LoadPlayback(string path)
    {
        using var reader = new StreamReader(path);
        var timeline = ReplayParser.ParseTimeline(reader);
        this.Player?.Dispose();
        this.Player = new ReplayPlayer(timeline, this.Registry);
        this.PlaybackPath = path;
        return $"Loaded {timeline.Ops.Count} ops ({this.Player.DurationSeconds:f0}s) for playback.";
    }

    /// <summary>Saved recordings, newest first (so an accidental record toggle can't hide an older one).</summary>
    public IReadOnlyList<string> ListRecordings()
    {
        try
        {
            var logs = Directory.GetFiles(this.directory, "minerva-*.log");
            Array.Sort(logs);
            Array.Reverse(logs);
            return logs;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Replay the loaded log through the matching module and report how well it covers the fight.</summary>
    public string ValidateModule()
    {
        if (this.PlaybackPath == null)
            return "No replay loaded to validate.";
        using var reader = new StreamReader(this.PlaybackPath);
        var timeline = ReplayParser.ParseTimeline(reader);
        return ReplayValidator.Validate(timeline, this.Registry).Render(new LuminaNameResolver());
    }

    /// <summary>
    /// Configure the loaded player to PREVIEW the generated draft — draw each cast's classified shape
    /// (from game-data resolution) without compiling anything. An approximation of the fixed-shape AOEs.
    /// </summary>
    public string BuildPreview()
    {
        if (this.Player == null || this.PlaybackPath == null)
            return "No replay loaded to preview.";
        this.LastFactSheet = this.Analyze(this.PlaybackPath); // refresh LastInput for the loaded log
        var input = this.LastInput;
        if (input == null)
            return "Could not analyze the recording.";

        var resolver = new LuminaShapeResolver();
        var shapes = new Dictionary<uint, PreviewCast>();
        foreach (var act in input.Actions)
        {
            var hint = resolver.Resolve(act.AID);

            // spread / stack / bait land on the marked player (a circle that follows them)
            if (act.Target == TargetKind.Player)
            {
                if (act.PlayerMechanic is PlayerMechanic.Spread or PlayerMechanic.Stack or PlayerMechanic.Bait)
                {
                    var r = hint.Kind == ShapeKind.Circle && hint.Radius > 0f ? hint.Radius : 6f;
                    shapes[act.AID] = new PreviewCast(new AOEShapeCircle(r), PreviewKind.OnTarget);
                }
                continue; // tankbusters have no zone
            }

            var shape = hint.ToShape();
            if (shape == null)
                continue;
            // a big self circle is a raidwide the module only hints — don't fill the arena with it
            if (hint.Kind == ShapeKind.Circle && act.Target == TargetKind.Self && hint.Radius >= 35f)
                continue;

            // a tether-preceded cast erupts on the tethered target, not the caster
            var kind = act.PrecedingTether != 0 ? PreviewKind.Tether : PreviewKind.Simple;
            shapes[act.AID] = new PreviewCast(shape, kind, act.PrecedingTether);
        }

        var c = input.Arena.Center;
        var half = input.Arena.HalfExtent;
        ArenaBounds bounds = half <= 0.1f
            ? new ArenaBoundsSquare(20f)
            : input.Arena.LooksSquare ? new ArenaBoundsSquare(MathF.Ceiling(half)) : new ArenaBoundsCircle(MathF.Ceiling(half));

        this.Player.SetPreview(shapes, new WPos(c.X, c.Z), bounds);
        return $"Preview built: {shapes.Count} draft AOE shapes. Play to watch the would-be module.";
    }

    /// <summary>Re-run analysis + generation on the loaded playback log (e.g. after an extractor change).</summary>
    public string RegenerateFromPlayback()
    {
        if (this.PlaybackPath == null)
            return "No replay loaded to analyze.";
        this.LastFactSheet = this.Analyze(this.PlaybackPath); // refreshes LastInput
        return this.GenerateModule();
    }

    /// <summary>Replay a saved log offline and render its fact sheet; also caches structured facts for generation.</summary>
    public string Analyze(string path)
    {
        using var reader = new StreamReader(path);
        ReplayAnalysis? analysis = null;
        ReplayParser.Replay(reader, ws => analysis = ReplayAnalysis.Attach(ws));
        this.LastInput = analysis?.BuildGenerationInput();
        return analysis?.Render() ?? "(no data)";
    }

    /// <summary>Generate a module draft from the last analyzed recording, using game-data shape inference.</summary>
    public string GenerateModule()
    {
        if (this.LastInput == null)
            return "No analyzed recording yet — record and stop a fight first.";

        var generator = new ModuleGenerator(new LuminaShapeResolver(), new LuminaNameResolver());
        var result = generator.Generate(this.LastInput);

        var path = Path.Combine(this.directory, $"D{this.LastInput.CFCID}.generated.cs");
        File.WriteAllText(path, result.Code);
        this.LastGeneratedPath = path;
        Service.Log.Information($"Minerva: generated module -> {path}\n{result.Report}");
        return result.Report + $"\nWritten to {path}";
    }

    /// <summary>Open the recordings/generated-modules folder in the OS file browser.</summary>
    public void OpenFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo(this.directory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Minerva: could not open the recordings folder.");
        }
    }

    /// <summary>Open the OS file browser with <paramref name="path"/> selected.</summary>
    public void RevealFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Minerva: could not reveal file.");
        }
    }

    /// <summary>Advance the loaded playback (called each frame by the plugin).</summary>
    public void UpdatePlayback(TimeSpan realDt) => this.Player?.Update(realDt);

    public void Dispose()
    {
        Service.DutyState.DutyCompleted -= this.OnDutyCompleted;
        this.actorDeathSubscription.Dispose();
        this.recorder?.Dispose();
        this.writer?.Dispose();
        this.Player?.Dispose();
    }
}
