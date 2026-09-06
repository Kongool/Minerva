using System;
using System.Diagnostics;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Minerva.Automation;
using Minerva.GameSync;
using Minerva.Modules;
using Minerva.Replay;
using Minerva.Windows;

namespace Minerva;

/// <summary>
/// Minerva plugin entry point. Phase 2: constructs the game-free <see cref="WorldState"/> and a
/// <see cref="WorldStateGameSync"/> that mirrors the live game into it every frame. A debug
/// window shows the mirrored state to verify the sync. Radar/modules/automation layer on later.
/// </summary>
public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/minerva";
    private const string CommandAlias = "/mine";

    public Configuration Config { get; }
    public WorldState World { get; }

    private readonly WorldStateGameSync sync;
    private readonly ModuleManager modules;
    private readonly MovementController movement;
    private readonly AIManager ai;
    private readonly ReplayService replay;
    private readonly MinervaIpc ipc;
    private readonly Automation.DaedalusRosterIPC roster;
    private ModuleBase? lastModule; // tracks the active-module transition for auto-show/hide of the radar
    private readonly WindowSystem windowSystem = new("Minerva");
    private readonly MainWindow mainWindow;
    private readonly WorldStateDebugWindow debugWindow;

    /// <summary>Named dodge-behaviour presets; also the slot another plugin can claim.</summary>
    public DodgePresets Presets { get; }
    private readonly DebugRadarWindow sandboxWindow;
    private readonly ArenaOverlayWindow arenaWindow;
    private readonly EncounterSettingsWindow encounterSettingsWindow;
    private readonly EncounterConfigStore encounterConfig;
    private readonly DtrStatus dtr;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        this.Config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        this.World = new WorldState((ulong)Stopwatch.Frequency, "unknown");
        this.sync = new WorldStateGameSync(this.World);
        this.modules = new ModuleManager(this.World);
        this.movement = new MovementController(this.Config);
        // constructed here, not as a field initializer: field initializers run BEFORE the constructor
        // body, so before Create<Service>() — and this reaches Service.PluginInterface, which is still
        // null at that point. That threw inside the plugin's own ctor and Dalamud refused to load it.
        this.roster = new Automation.DaedalusRosterIPC();
        this.ai = new AIManager(this.World, this.modules, this.Config, this.movement, new Minerva.Generation.LuminaShapeResolver());
        this.ai.Roster = this.roster;
        this.replay = new ReplayService(this.World, this.modules, this.Config);
        this.Presets = new DodgePresets(this.Config);
        this.ipc = new MinervaIpc(Service.PluginInterface, this.ai, this.modules, this.Presets);

        // Config nodes live next to the modules that read them, i.e. in this assembly, so the root has
        // to be told about it before anything constructs a module.
        ConfigRoot.Instance.Discover(typeof(Plugin).Assembly);
        this.encounterConfig = new EncounterConfigStore(ConfigRoot.Instance, Service.PluginInterface.ConfigDirectory.FullName);

        this.mainWindow = new MainWindow(this, this.modules, this.ai, this.replay);
        this.debugWindow = new WorldStateDebugWindow(this.World);
        this.sandboxWindow = new DebugRadarWindow();
        this.arenaWindow = new ArenaOverlayWindow(this, this.modules, this.ai, this.Config);
        this.encounterSettingsWindow = new EncounterSettingsWindow(ConfigRoot.Instance);
        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.debugWindow);
        this.windowSystem.AddWindow(this.sandboxWindow);
        this.windowSystem.AddWindow(this.arenaWindow);
        this.windowSystem.AddWindow(this.encounterSettingsWindow);

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Minerva settings. Also: radar, modules, record, strats, debug, sandbox, face <dir>.",
        });
        Service.CommandManager.AddHandler(CommandAlias, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Shortcut for /minerva.",
        });

        this.dtr = new DtrStatus(this.modules, this.replay, this.Config, this.OpenMain);

        Service.Framework.Update += this.OnUpdate;
        Service.PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenMainUi += this.OpenMain;
        Service.PluginInterface.UiBuilder.OpenConfigUi += this.OpenMain;

        ModuleBase.ErrorSink = static msg => Service.Log.Warning($"[ModuleError] {msg}");

        Service.Log.Information("Minerva loaded.");
    }

    private void OnUpdate(IFramework framework)
    {
        try
        {
            this.sync.Update(framework.UpdateDelta);
            this.modules.Update();
            this.ai.Update();
            this.ipc.Update();
            this.roster.Update(DateTime.Now);
            var recordingResult = this.replay.UpdateRecording(framework.UpdateDelta);
            if (recordingResult != null)
            {
                Service.ChatGui.Print("[Minerva] " + recordingResult);
                this.mainWindow.Open("Record");
            }
            this.replay.UpdatePlayback(framework.UpdateDelta);
            this.SyncRadarVisibility();
            this.dtr.Update();
            this.encounterConfig.Flush();
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Minerva sync tick failed.");
        }
    }

    /// <summary>
    /// BMR-style pull behaviour: pop the radar open when a boss module activates, and (optionally)
    /// close it again when the module tears down. Driven off the active-module transition so it fires
    /// exactly once per pull, and only reacts to the null↔active edges — never fighting a manual toggle
    /// while a module stays active.
    /// </summary>
    private void SyncRadarVisibility()
    {
        var current = this.modules.ActiveModule;
        if (ReferenceEquals(current, this.lastModule))
            return;

        // The overlay is what pops for a fight -- it is only the arena, so it can appear without covering
        // anything you were reading. The main window is left alone: it holds settings and tooling and has
        // no business opening itself because a pull started.
        if (current != null)
        {
            if (this.Config.AutoShowRadar)
                this.arenaWindow.IsOpen = true;
        }
        else if (this.Config.AutoHideRadar)
        {
            this.arenaWindow.IsOpen = false;
        }

        this.lastModule = current;
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "radar": this.ToggleRadar(); break;
            case "settings" or "menu" or "console" or "tabs": this.mainWindow.Open("Settings"); break;
            case "modules" or "bosses": this.mainWindow.Open("Modules"); break;
            // "record" shows the recorder rather than arming it: typing a word should show you where you
            // are, not silently start capturing. The button on that tab still toggles.
            case "record" or "recordings" or "replay": this.mainWindow.Open("Record"); break;
            case "record now": this.ToggleRecording(); break;
            case "strats" or "strategies": this.ToggleEncounterSettings(); break;
            case "debug": this.ToggleDebug(); break;
            case "sandbox": this.ToggleSandbox(); break;
            case var s when s.StartsWith("face ", StringComparison.Ordinal): this.FaceHeading(s[5..]); break;
            default: this.ToggleMenu(); break;
        }
    }

    // --- UI actions: shared by the /minerva subcommands and the main-window buttons ---

    /// <summary>Toggle the arena overlay — what "the radar" means to anyone using it in a fight.</summary>
    public void ToggleRadar() => this.arenaWindow.Toggle();

    /// <summary>Open the arena overlay. Used by the Settings tab's "Radar" button.</summary>
    public void ShowRadar() => this.arenaWindow.IsOpen = true;

    public void ToggleDebug() => this.debugWindow.Toggle();

    /// <summary>The main window (settings, modules, recordings) — what a bare /mine toggles.</summary>
    public void ToggleMenu() => this.mainWindow.Toggle();

    public void ToggleEncounterSettings() => this.encounterSettingsWindow.Toggle();

    /// <summary>
    /// Turn to a cardinal point or a compass bearing: <c>/mine face nw</c>, <c>/mine face 210</c>.
    /// <para>Bearings are read the way a compass is, 0 = north, clockwise — not the game's internal angle,
    /// where 0 is south. Typing "face 90" should point you east, whatever the engine calls that.</para>
    /// </summary>
    private void FaceHeading(string arg)
    {
        arg = arg.Trim();
        float? compass = arg.ToLowerInvariant() switch
        {
            "n" or "north" => 0f,
            "ne" or "northeast" => 45f,
            "e" or "east" => 90f,
            "se" or "southeast" => 135f,
            "s" or "south" => 180f,
            "sw" or "southwest" => 225f,
            "w" or "west" => 270f,
            "nw" or "northwest" => 315f,
            _ => float.TryParse(arg, System.Globalization.CultureInfo.InvariantCulture, out var deg) ? deg : null,
        };

        if (compass == null)
        {
            Service.ChatGui.Print("Minerva: face what? Try a cardinal (n, ne, e...) or a bearing in degrees.");
            return;
        }

        // compass 0=north clockwise -> game 0=south counter-clockwise
        var game = (180f - compass.Value) * (MathF.PI / 180f);
        if (Minerva.GameSync.GameData.TryFace(game))
            Service.ChatGui.Print($"Minerva: facing {compass.Value:0.#}°.");
        else
            Service.ChatGui.Print("Minerva: could not turn — no player object.");
    }
    public void ToggleSandbox() => this.sandboxWindow.Toggle();

    /// <summary>Whether a replay is currently being recorded (drives the record button label).</summary>
    public bool IsRecording => this.replay.IsRecording;

    /// <summary>Start/stop recording and echo the result to chat; by default also surface the Record page.
    /// The radar's own REC button passes false: mid-fight, a settings window popping over the arena is the
    /// last thing you want.</summary>
    public void ToggleRecording(bool showRecord = true)
    {
        Service.ChatGui.Print("[Minerva] " + this.replay.Toggle());
        if (showRecord)
            this.mainWindow.Open("Record");
    }

    /// <summary>The server-bar click and Dalamud's own open-config/open-main entries all land on Settings.</summary>
    private void OpenMain() => this.mainWindow.Open("Settings");

    public void Dispose()
    {
        Service.Framework.Update -= this.OnUpdate;
        Service.PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMain;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenMain;
        this.windowSystem.RemoveAllWindows();
        this.mainWindow.Dispose();
        this.debugWindow.Dispose();
        this.sandboxWindow.Dispose();
        this.arenaWindow.Dispose();
        this.encounterConfig.Dispose();
        this.dtr.Dispose();
        this.ipc.Dispose();
        this.replay.Dispose();
        this.movement.Dispose();
        this.modules.Dispose();
        this.sync.Dispose();
        Service.CommandManager.RemoveHandler(CommandName);
        Service.CommandManager.RemoveHandler(CommandAlias);
    }
}
