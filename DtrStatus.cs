using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Minerva.Modules;
using Minerva.Replay;
using Minerva.Windows;

namespace Minerva;

/// <summary>
/// Minerva's two entries on the server info bar.
///
/// <para><b>Mine:</b> what Minerva thinks is happening — the boss it has a module for, or <c>Idle</c>, with
/// a red dot while recording. Left-click opens the config. This is the answer to the question that comes up
/// every session: <em>did the module actually activate?</em> Until now that took opening the radar, which
/// is the wrong instrument for a yes/no you want mid-pull across four clients.</para>
///
/// <para><b>Mine AI:</b> whether auto-dodge is steering. Left-click toggles it. Boxing means turning the
/// dodge off on one toon without alt-tabbing through four config windows to do it.</para>
///
/// <para>Text is rebuilt only when it changes: the bar re-lays out every entry when one is written, so a
/// plugin that assigns an identical SeString every frame makes the whole bar jitter.</para>
/// </summary>
internal sealed class DtrStatus : IDisposable
{
    // UIColor rows, the same palette the game's own info bar uses.
    private const ushort ColorActive = 43;  // green
    private const ushort ColorIdle = 3;     // grey
    private const ushort ColorRecording = 17; // red

    /// <summary>Longest boss name shown before it is cut. The bar is shared with every other plugin, and a
    /// full name like "Blood-red Marauder" pushes the clock off the end of a crowded one.</summary>
    private const int MaxNameLength = 18;

    private readonly IDtrBarEntry? stateEntry;
    private readonly IDtrBarEntry? aiEntry;
    private readonly ModuleManager modules;
    private readonly ReplayService replay;
    private readonly Configuration config;

    private string lastState = string.Empty;
    private bool lastRecording;
    private bool lastAi;
    private bool lastShown;
    private bool primed;

    public DtrStatus(ModuleManager modules, ReplayService replay, Configuration config, Action openConfig)
    {
        this.modules = modules;
        this.replay = replay;
        this.config = config;

        this.stateEntry = TryGet("Minerva");
        this.aiEntry = TryGet("Minerva AI");

        if (this.stateEntry != null)
            this.stateEntry.OnClick = ev =>
            {
                if (ev.ClickType == MouseClickType.Left)
                    openConfig();
            };

        if (this.aiEntry != null)
            this.aiEntry.OnClick = ev =>
            {
                if (ev.ClickType != MouseClickType.Left)
                    return;
                // Written straight through to the config file rather than held in memory: the toggle is
                // most useful right before a pull, which is also the moment a crash or a /xlreload loses it.
                this.config.AutoDodgeEnabled = !this.config.AutoDodgeEnabled;
                this.config.Save();
            };
    }

    /// <summary>
    /// Claim a slot on the bar, or carry on without one.
    ///
    /// <para>Dalamud throws if another plugin already owns the title, and this is constructed from the
    /// plugin's own constructor — so an unguarded call means a name collision stops Minerva loading at all.
    /// A status readout is not worth that trade, and this plugin has already been refused a load once by
    /// exactly this shape of constructor-time throw.</para>
    /// </summary>
    private static IDtrBarEntry? TryGet(string title)
    {
        try
        {
            return Service.DtrBar.Get(title);
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, $"Minerva: could not claim the '{title}' server info bar entry.");
            return null;
        }
    }

    public void Update()
    {
        if (this.stateEntry == null && this.aiEntry == null)
            return;

        // Hidden rather than removed: releasing the slots would lose the position the user sorted them
        // into in Dalamud's settings, and re-claiming can fail. Text keeps updating while hidden so the
        // entries are correct the moment they come back rather than showing the last state before the tick.
        var show = this.config.ShowDtrBar;
        if (!this.primed || show != this.lastShown)
        {
            if (this.stateEntry != null)
                this.stateEntry.Shown = show;
            if (this.aiEntry != null)
                this.aiEntry.Shown = show;
            this.lastShown = show;
        }

        var state = this.StateText();
        var recording = this.replay.IsRecording;
        var ai = this.config.AutoDodgeEnabled;

        if (this.stateEntry != null && (!this.primed || state != this.lastState || recording != this.lastRecording))
        {
            var sb = new SeStringBuilder().AddText("Mine: ");
            if (recording)
                sb.AddUiForeground(ColorRecording).AddText("\u25cf ").AddUiForegroundOff();
            this.stateEntry!.Text = sb
                .AddUiForeground(state == "Idle" ? ColorIdle : ColorActive)
                .AddText(state)
                .AddUiForegroundOff()
                .Build();
            this.stateEntry.Tooltip = this.StateTooltip(recording);
            this.lastState = state;
            this.lastRecording = recording;
        }

        if (this.aiEntry != null && (!this.primed || ai != this.lastAi))
        {
            this.aiEntry!.Text = new SeStringBuilder()
                .AddText("Mine AI: ")
                .AddUiForeground(ai ? ColorActive : ColorIdle)
                .AddText(ai ? "On" : "Off")
                .AddUiForegroundOff()
                .Build();
            this.aiEntry.Tooltip = ai
                ? "Auto-dodge is steering this character.\nClick to turn it off."
                : "Auto-dodge is off — the radar still draws, nothing moves you.\nClick to turn it on.";
            this.lastAi = ai;
        }

        this.primed = true;
    }

    /// <summary>The boss Minerva has a module for, or <c>Idle</c>. Named, not just "active", because which
    /// module landed is the thing worth checking — a fight with two candidate modules can pick the wrong
    /// one, and that reads identically to "working" on any status that only counts.</summary>
    private string StateText()
    {
        if (this.modules.ActiveModuleInfo is not { } info)
            return "Idle";

        var name = ModulesTab.ResolveBossName(info.Attr.NameID) ?? ModulesTab.Prettify(info.ModuleType.Name);
        return name.Length > MaxNameLength ? name[..(MaxNameLength - 1)] + "\u2026" : name;
    }

    private SeString StateTooltip(bool recording)
    {
        var sb = new System.Text.StringBuilder();
        if (this.modules.ActiveModuleInfo is { } info)
            sb.Append("Module: ").Append(info.ModuleType.Name);
        else
            sb.Append("No boss module active.");

        if (this.modules.ActiveZoneModule is { } zone)
            sb.Append("\nZone: ").Append(zone.GetType().Name);
        if (recording)
            sb.Append("\nRecording.");

        sb.Append("\n\nClick to open Minerva.");
        return sb.ToString();
    }

    public void Dispose()
    {
        this.stateEntry?.Remove();
        this.aiEntry?.Remove();
    }
}
