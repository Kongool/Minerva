using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Dalamud.Game.DutyState;
using Minerva.GameSync;
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

    /// <summary>
    /// Out of combat this long and the recording ends itself.
    /// <para>Long enough to sit through a wipe-and-rez or the lull between an add pack and the boss, short
    /// enough that an abandoned pull does not append ten minutes of walking to the file. It exists because
    /// starting on combat and only stopping on a death detection leaves every other ending — a despawn, a
    /// reset, giving up — recording forever.</para>
    /// </summary>
    private static readonly TimeSpan OutOfCombatEndsRecording = TimeSpan.FromSeconds(8);

    private readonly WorldState world;
    private readonly ModuleManager modules;
    private readonly Configuration config;
    private readonly string directory;
    private readonly RecordingCompletionDetector recordingCompletion = new(EncounterEndGracePeriod, OutOfCombatEndsRecording);
    private readonly EventSubscription actorDeathSubscription;

    private ReplayRecorder? recorder;
    private StreamWriter? writer;
    private string? currentPath;

    // no-module fallback: a recording made to EXTRACT a module has no module loaded, so OnActorDeath can't
    // fire. We then identify the boss heuristically as the biggest in-combat enemy and feed the same
    // completion signal when it dies.
    private ulong recordingBossId;
    private uint recordingBossMaxHP;
    private float recordingBossLastHP;
    private bool recordingBossSeen;
    private Actor? recordingPrimary;      // the module's boss, kept so its death is still detectable after
    private bool recordingPrimaryEnds;    // ... the module deactivates and takes ActiveModule with it
    private string? recordingEncounterName; // Occult CE name, when the fight came from the director

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
        this.recordingBossId = 0;
        this.recordingBossMaxHP = 0;
        this.recordingBossLastHP = 0f;
        this.recordingBossSeen = false;
        this.recordingPrimary = null;
        this.recordingPrimaryEnds = false;
        this.recordingEncounterName = null;
    }

    // make a boss name safe for a filename (keeps spaces/hyphens; replaces only the truly-invalid chars)
    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
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

        // rename the log to include the boss name now that analysis knows it — the recording picker shows
        // filenames, and a timestamp alone is unreadable. Kept the timestamp prefix so sorting stays intact.
        try
        {
            // Prefer the encounter name where there is one: Occult content is known by its critical
            // encounter, not by whichever enemy happens to be biggest, and that is also how the modules for
            // it are filed. Falls back to the boss the analysis identified everywhere else.
            var boss = this.recordingEncounterName ?? this.LastInput?.BossName;
            if (!string.IsNullOrWhiteSpace(boss) && this.currentPath != null)
            {
                var renamed = Path.Combine(this.directory, $"{Path.GetFileNameWithoutExtension(this.currentPath)}-{Sanitize(boss)}.log");
                if (!string.Equals(renamed, this.currentPath, StringComparison.OrdinalIgnoreCase) && !File.Exists(renamed))
                {
                    File.Move(this.currentPath, renamed);
                    this.currentPath = renamed;
                    this.LastPath = renamed;
                }
            }
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Minerva: could not rename recording with the boss name.");
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
        if (!this.IsRecording)
        {
            // Start on a module activating OR on an unscripted boss appearing. The second case is the one
            // that matters for authoring: a fight with no module yet is exactly the fight you need a
            // recording of, and Occult Crescent's critical engagements are named after the FATE rather than
            // the boss, so they can't be recognised by name either. Stopping is already handled — the
            // completion detector ends it on the boss's death, module or not.
            if (this.config.AutoRecordEncounters)
            {
                var module = this.modules.ActiveModule;
                if (module != null)
                {
                    this.Start();
                    return $"Auto-recording {module.GetType().Name} to {Path.GetFileName(this.currentPath)}.";
                }

                if (this.UnknownBoss() is { } boss)
                {
                    this.Start();

                    // Named after the boss, which is the one thing here that is verifiable. A critical
                    // encounter's own name would read better — that is what the content is known by — but
                    // the encounter director reports every event in the zone, so it cannot say whether
                    // THIS player is in THAT one. Labelling from it produced a recording of the Regnant
                    // Chimera FATE filed under 'Familiar Tactics', a CE running elsewhere on the map.
                    this.recordingEncounterName = boss.Name;
                    return $"Auto-recording unscripted boss '{boss.Name}' to {Path.GetFileName(this.currentPath)}.";
                }
            }

            return null;
        }

        this.DetectModuleBossDeath();
        this.DetectNoModuleBossDeath();
        this.recordingCompletion.NoteCombat(this.modules.LocalPlayer() is { InCombat: true }, realDt);
        if (!this.recordingCompletion.Update(realDt))
            return null;

        var opCount = this.Stop();
        return $"Encounter ended. Recording stopped automatically: {opCount} ops -> {Path.GetFileName(this.LastPath)}. Fact sheet ready.";
    }

    /// <summary>
    /// A big enemy, near the player, that the player is fighting. The rule itself lives in
    /// <see cref="EncounterTrigger"/> so it can be tested without a game.
    /// </summary>
    private Actor? UnknownBoss() => EncounterTrigger.NearbyEngagedBoss(this.modules.LocalPlayer(), this.world.Actors);

    private void OnActorDeath(Actor actor)
    {
        var active = this.modules.ActiveModule;
        if (this.IsRecording && actor.IsDead && active != null && ReferenceEquals(actor, active.PrimaryActor)
            && this.modules.ActiveModuleInfo?.Attr.PrimaryActorDeathEndsEncounter == true)
            this.recordingCompletion.SignalCompletion();
    }

    /// <summary>
    /// Fallback for recordings made to EXTRACT a module, where no module is loaded so <see cref="OnActorDeath"/>
    /// can't identify the boss. The boss is taken to be the biggest <b>in-combat</b> enemy (the in-combat gate
    /// keeps ambient open-field NMs out); it re-locks to an equal-or-larger enemy so it survives the boss
    /// appearing after the pull and same-size phase swaps. When it dies (0 HP, or despawns while near death —
    /// a mid-HP despawn is treated as a phase change, not a kill) it feeds the same completion signal the
    /// module/duty paths use, so the 2s grace period and stop behaviour are identical. Skipped entirely once a
    /// module is active, since OnActorDeath then handles the boss precisely.
    /// </summary>
    /// <summary>
    /// End the recording when the module's boss dies, by asking every frame rather than by catching the
    /// moment it happens.
    /// <para><see cref="OnActorDeath"/> already listens for the death event, and a Thundergust Griffin
    /// recording still ran nine seconds past a boss that verifiably died — the event is one frame wide, and
    /// anything that misses it (the module deactivating in the same tick and taking
    /// <c>ActiveModule</c> with it, a subscription that was not live yet) leaves nothing to fall back on
    /// but the idle timeout. A remembered reference and a poll cannot be missed, and re-signalling is
    /// harmless because the detector only completes once.</para>
    /// </summary>
    private void DetectModuleBossDeath()
    {
        if (this.modules.ActiveModule is { PrimaryActor: { } primary })
        {
            this.recordingPrimary = primary;
            this.recordingPrimaryEnds = this.modules.ActiveModuleInfo?.Attr.PrimaryActorDeathEndsEncounter == true;
        }

        if (this.recordingPrimaryEnds && this.recordingPrimary is { } boss && (boss.IsDead || boss.HPMP.CurHP == 0))
            this.recordingCompletion.SignalCompletion();
    }

    private void DetectNoModuleBossDeath()
    {
        if (this.modules.ActiveModule != null)
            return;

        Actor? biggest = null;
        foreach (var a in this.world.Actors)
        {
            if (a.IsAlly || !a.InCombat || a.IsDeadOrDestroyed || a.HPMP.MaxHP == 0 || a.HPMP.CurHP == 0)
                continue;
            if (biggest == null || a.HPMP.MaxHP > biggest.HPMP.MaxHP)
                biggest = a;
        }

        if (biggest != null && biggest.HPMP.MaxHP >= this.recordingBossMaxHP)
        {
            this.recordingBossId = biggest.InstanceID;
            this.recordingBossMaxHP = biggest.HPMP.MaxHP;
            this.recordingBossSeen = true;
        }

        if (!this.recordingBossSeen)
            return;

        var tracked = this.world.Actors.Find(this.recordingBossId);
        bool bossDead;
        if (tracked == null)
            bossDead = this.recordingBossLastHP <= 0.05f; // vanished while healthy = phase change, not a kill
        else
        {
            this.recordingBossLastHP = tracked.HPRatio;
            bossDead = tracked.IsDead || tracked.HPMP.CurHP == 0;
        }

        var nothingBiggerAlive = biggest == null || biggest.HPMP.MaxHP < this.recordingBossMaxHP;
        if (bossDead && nothingBiggerAlive)
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
        this.Player = new ReplayPlayer(timeline, this.Registry, this.config);
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

        var path = Path.Combine(this.directory, this.LastInput.DraftFileName());
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
