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
    private ModuleBase? lastModule; // tracks the active-module transition for auto-show/hide of the radar
    private readonly WindowSystem windowSystem = new("Minerva");
    private readonly MainWindow mainWindow;
    private readonly WorldStateDebugWindow debugWindow;
    private readonly RadarWindow radarWindow;
    private readonly ReplayWindow replayWindow;
    private readonly BossBrowserWindow bossesWindow;

    /// <summary>Named dodge-behaviour presets; also the slot another plugin can claim.</summary>
    public DodgePresets Presets { get; }
    private readonly DebugRadarWindow sandboxWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        this.Config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        this.World = new WorldState((ulong)Stopwatch.Frequency, "unknown");
        this.sync = new WorldStateGameSync(this.World);
        this.modules = new ModuleManager(this.World);
        this.movement = new MovementController(this.Config);
        this.ai = new AIManager(this.World, this.modules, this.Config, this.movement, new Minerva.Generation.LuminaShapeResolver());
        this.replay = new ReplayService(this.World, this.modules, this.Config);
        this.Presets = new DodgePresets(this.Config);
        this.ipc = new MinervaIpc(Service.PluginInterface, this.ai, this.modules, this.Presets);

        this.mainWindow = new MainWindow(this);
        this.debugWindow = new WorldStateDebugWindow(this.World);
        this.replayWindow = new ReplayWindow(this.replay);
        this.bossesWindow = new BossBrowserWindow(this.modules);
        this.radarWindow = new RadarWindow(this.modules, this.ai, this.Config, this.debugWindow, this.mainWindow, this.replayWindow);
        this.sandboxWindow = new DebugRadarWindow();
        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.debugWindow);
        this.windowSystem.AddWindow(this.radarWindow);
        this.windowSystem.AddWindow(this.replayWindow);
        this.windowSystem.AddWindow(this.bossesWindow);
        this.windowSystem.AddWindow(this.sandboxWindow);

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Radar. Also: sandbox (test radar, no duty), debug (inspector), replay, record, menu.",
        });
        Service.CommandManager.AddHandler(CommandAlias, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Shortcut for /minerva (toggles the radar; same subcommands).",
        });

        Service.Framework.Update += this.OnUpdate;
        Service.PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenMainUi += this.OpenMain;
        Service.PluginInterface.UiBuilder.OpenConfigUi += this.OpenMain;

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
            var recordingResult = this.replay.UpdateRecording(framework.UpdateDelta);
            if (recordingResult != null)
            {
                Service.ChatGui.Print("[Minerva] " + recordingResult);
                this.replayWindow.IsOpen = true;
            }
            this.replay.UpdatePlayback(framework.UpdateDelta);
            this.SyncRadarVisibility();
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

        if (current != null)
        {
            if (this.Config.AutoShowRadar)
                this.radarWindow.IsOpen = true;
        }
        else if (this.Config.AutoHideRadar)
        {
            this.radarWindow.IsOpen = false;
        }

        this.lastModule = current;
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "debug": this.ToggleDebug(); break;
            case "menu": this.ToggleMenu(); break;
            case "sandbox": this.ToggleSandbox(); break;
            // "record" opens the recordings window rather than arming the recorder: typing a word should
            // show you where you are, not silently start capturing. The button in that window still toggles.
            case "record" or "recordings" or "replay": this.ToggleReplay(); break;
            case "bosses" or "modules": this.ToggleBosses(); break;
            case var s when s.StartsWith("face ", StringComparison.Ordinal): this.FaceHeading(s[5..]); break;
            case "record now": this.ToggleRecording(); break;
            default: this.ToggleRadar(); break;
        }
    }

    // --- UI actions: shared by the /minerva subcommands and the main-window buttons ---
    public void ToggleRadar() => this.radarWindow.Toggle();

    /// <summary>Open the radar window (if closed) and switch to its Radar tab. Used by the Menu's "Radar"
    /// button so it never just closes the window it's embedded in.</summary>
    public void ShowRadar()
    {
        this.radarWindow.IsOpen = true;
        this.radarWindow.SelectTab("Radar");
    }
    public void ToggleDebug() => this.debugWindow.Toggle();
    public void ToggleMenu() => this.mainWindow.Toggle();
    public void ToggleReplay() => this.replayWindow.Toggle();
    public void ToggleBosses() => this.bossesWindow.Toggle();

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

    /// <summary>Start/stop recording, echo the result to chat, and surface the replay window.</summary>
    /// <remarks>
    /// IsOpen alone is not "show me": a window that was collapsed to its title bar, or is sitting behind
    /// the menu that owns the button, stays exactly where it was and the click reads as having done
    /// nothing. Uncollapse and raise it as well.
    /// </remarks>
    public void ToggleRecording()
    {
        Service.ChatGui.Print("[Minerva] " + this.replay.Toggle());
        this.replayWindow.IsOpen = true;
        this.replayWindow.Collapsed = false;
        this.replayWindow.BringToFront();
    }

    private void OpenMain() => this.mainWindow.IsOpen = true;

    public void Dispose()
    {
        Service.Framework.Update -= this.OnUpdate;
        Service.PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        Service.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMain;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenMain;
        this.windowSystem.RemoveAllWindows();
        this.mainWindow.Dispose();
        this.debugWindow.Dispose();
        this.radarWindow.Dispose();
        this.replayWindow.Dispose();
        this.sandboxWindow.Dispose();
        this.ipc.Dispose();
        this.replay.Dispose();
        this.movement.Dispose();
        this.modules.Dispose();
        this.sync.Dispose();
        Service.CommandManager.RemoveHandler(CommandName);
        Service.CommandManager.RemoveHandler(CommandAlias);
    }
}
