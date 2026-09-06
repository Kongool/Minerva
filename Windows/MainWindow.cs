using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Minerva.Automation;
using Minerva.Modules;
using Minerva.Replay;

namespace Minerva.Windows;

/// <summary>
/// The main window: a ribbon — Settings · Modules · Record as a segmented switch, and the plugin's state as
/// pills — over one page. The radar is deliberately not a page: it lives in <see cref="ArenaOverlayWindow"/>,
/// small and square, and pops for a pull.
/// </summary>
public sealed class MainWindow : Window, IDisposable
{
    private static readonly string[] Pages = ["Settings", "Modules", "Record"];

    private readonly Plugin plugin;
    private readonly ModuleManager manager;
    private readonly AIManager ai;
    private readonly ReplayService replay;
    private readonly ModulesTab modules;
    private readonly ReplayTab record;
    private string page = "Settings";
    private string presetName = string.Empty;

    public MainWindow(Plugin plugin, ModuleManager manager, AIManager ai, ReplayService replay)
        : base("Minerva###MinervaMain")
    {
        this.plugin = plugin;
        this.manager = manager;
        this.ai = ai;
        this.replay = replay;
        this.modules = new ModulesTab(manager);
        this.record = new ReplayTab(replay);
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(640, 440),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        this.Size = new Vector2(780, 580);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    private (int Colors, int Vars) theme;

    public override void PreDraw() => this.theme = AegisTheme.Push();

    public override void PostDraw()
    {
        AegisTheme.Pop(this.theme);
        // Collapsed is not a one-off: Dalamud turns it into a SetNextWindowCollapsed on every frame it holds
        // a value, so the uncollapse Open() asks for has to be withdrawn once it has happened -- or the
        // collapse arrow stops working for good.
        this.Collapsed = null;
    }

    /// <summary>
    /// Open on <paramref name="page"/>, raised and uncollapsed. IsOpen alone is not "show me": a window
    /// collapsed to its title bar, or sitting behind another, stays where it was and the command reads as
    /// having done nothing.
    /// </summary>
    public void Open(string page)
    {
        this.IsOpen = true;
        this.Collapsed = false;
        this.BringToFront();
        this.page = page;
    }

    public override void Draw()
    {
        this.DrawRibbon();
        ImGui.Separator();

        // The page scrolls under a fixed ribbon. Guarded so a page that throws still balances the child and
        // is shown in place — an unbalanced stack is what turns a draw bug into a crash of the whole plugin.
        var open = ImGui.BeginChild("##page", new Vector2(0f, 0f), false);
        try
        {
            if (!open)
                return;
            switch (this.page)
            {
                case "Modules": this.modules.Draw(); break;
                case "Record": this.record.Draw(); break;
                default: this.DrawSettings(); break;
            }
        }
        catch (Exception ex)
        {
            ImGui.TextColored(UiKit.Red, $"{this.page} draw error: {ex.Message}");
            Service.Log.Error(ex, $"Minerva: {this.page} page draw threw.");
        }
        finally
        {
            ImGui.EndChild();
        }
    }

    /// <summary>The segmented switch, and the plugin's state as pills on the right — the server bar, expanded.</summary>
    private void DrawRibbon()
    {
        for (var i = 0; i < Pages.Length; ++i)
        {
            if (i != 0)
                ImGui.SameLine(0f, 0f);
            if (UiKit.Seg(Pages[i].ToUpperInvariant(), this.page == Pages[i]))
                this.page = Pages[i];
        }

        var cfg = this.plugin.Config;
        var active = this.manager.ActiveModuleInfo;
        var mine = active != null ? $"Mine: {ModulesTab.BossName(active)}" : "Mine: Idle";
        var aiText = cfg.AutoDodgeEnabled ? "AI On" : "AI Off";
        var backend = this.ai.Movement is MovementController mc ? mc.NavmeshBackend ?? "direct" : "direct";
        var recording = this.replay.IsRecording;

        // right-aligned; falls back to flowing after the switch when the window is too narrow
        var total = UiKit.PillWidth(mine) + UiKit.PillWidth(aiText) + UiKit.PillWidth(backend) + 12f;
        if (recording)
            total += UiKit.PillWidth("● REC") + 6f;
        ImGui.SameLine();
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), ImGui.GetWindowContentRegionMax().X - total));

        UiKit.Pill(mine, active != null ? AegisTheme.TyrianBright : AegisTheme.Rule, active != null ? AegisTheme.Travertine : AegisTheme.TravertineDim);
        ImGui.SameLine(0f, 6f);
        UiKit.Pill(aiText, cfg.AutoDodgeEnabled ? UiKit.Alpha(AegisTheme.Bronze, 0.7f) : AegisTheme.Rule, cfg.AutoDodgeEnabled ? AegisTheme.BronzeBright : AegisTheme.TravertineDim);
        ImGui.SameLine(0f, 6f);
        UiKit.Pill(backend, AegisTheme.Rule, AegisTheme.TravertineDim);
        if (recording)
        {
            ImGui.SameLine(0f, 6f);
            UiKit.Pill("● REC", UiKit.Alpha(UiKit.Red, 0.6f), new Vector4(1f, 0.6f, 0.6f, 1f));
        }
    }

    /// <summary>
    /// Settings as cards on a grid, so the whole page fits at once and each card names its own state.
    /// Help lives in tooltips.
    /// </summary>
    private void DrawSettings()
    {
        var cfg = this.plugin.Config;
        var changed = false;

        if (ImGui.BeginTable("##cards", 2, ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            UiKit.BeginCard("Radar", cfg.RadarEnabled ? "on" : "off");
            changed |= Grid2(
                ("Enabled", cfg.RadarEnabled, v => cfg.RadarEnabled = v, null),
                ("Open on pull", cfg.AutoShowRadar, v => cfg.AutoShowRadar = v, "Pops the radar when a known boss module activates."),
                ("Close after the fight", cfg.AutoHideRadar, v => cfg.AutoHideRadar = v, "Off leaves it up for post-pull review, as BossmodReborn does."),
                ("Confine AOEs", cfg.ClipToArena, v => cfg.ClipToArena = v, "Mask AOE fills past the arena edge."),
                ("Show party", cfg.ShowPartyMembers, v => cfg.ShowPartyMembers = v, "You are always drawn on top, in your own colour."),
                ("Server info bar", cfg.ShowDtrBar, v => cfg.ShowDtrBar = v, "Mine: state, and a Mine AI toggle — click either."));
            UiKit.EndCard();

            ImGui.TableNextColumn();
            UiKit.BeginCard("Look");
            if (ImGui.BeginTable("##look", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("k", ImGuiTableColumnFlags.WidthFixed, 110f);
                ImGui.TableSetupColumn("v", ImGuiTableColumnFlags.WidthStretch);

                Key("Opacity");
                var opacity = cfg.RadarOpacity;
                if (ImGui.SliderFloat("##opacity", ref opacity, 0f, 1f, "%.2f"))
                {
                    cfg.RadarOpacity = opacity;
                    changed = true;
                }
                UiKit.Tip("Background opacity. At 0 the window is see-through; the field keeps its own dark floor.");

                Key("Heading");
                var heading = (int)cfg.RadarHeading;
                if (ImGui.Combo("##radarheading", ref heading, "Static (north up)\0Camera align\0"))
                {
                    cfg.RadarHeading = (RadarHeading)heading;
                    changed = true;
                }

                if (cfg.ShowPartyMembers)
                {
                    Key("Colour party by");
                    var mode = (int)cfg.PartyColorBy;
                    if (ImGui.Combo("##partycolor", ref mode, "Uniform\0Role (tank/healer/melee/ranged)\0Light party (1 / 2)\0"))
                    {
                        cfg.PartyColorBy = (PartyColoring)mode;
                        changed = true;
                    }
                }

                ImGui.EndTable();
            }
            UiKit.EndCard();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            UiKit.BeginCard("Auto-dodge", cfg.AutoDodgeEnabled ? "moving" : "guidance only");
            changed |= Grid2(
                ("Guidance marker", cfg.AutoDodgeGuidance, v => cfg.AutoDodgeGuidance = v, "Draw the safe spot on the radar."),
                ("Unscripted content", cfg.AutoHintsForTrash, v => cfg.AutoHintsForTrash = v, "Also dodge trash and open-world casts, guessed from enemy cast bars when no boss module is active."),
                ("Auto-move", cfg.AutoDodgeEnabled, v => cfg.AutoDodgeEnabled = v, "Steer your character to the safe spot. Experimental."),
                ("Face gazes", cfg.AutoFaceGazes, v => cfg.AutoFaceGazes = v, "Turn your character away from gazes. Experimental."),
                ("Navmesh", cfg.UseNavmesh, v => cfg.UseNavmesh = v, "Path around walls with Ariadne or vnavmesh when available; falls back to direct steering."));
            if (ImGui.BeginTable("##dodge", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("k", ImGuiTableColumnFlags.WidthFixed, 110f);
                ImGui.TableSetupColumn("v", ImGuiTableColumnFlags.WidthStretch);

                Key("Safety margin");
                var margin = cfg.AutoDodgeSafetyMargin;
                if (ImGui.SliderFloat("##margin", ref margin, 0f, 10f, "%.1f y"))
                {
                    cfg.AutoDodgeSafetyMargin = Math.Clamp(margin, 0f, 10f);
                    changed = true;
                }
                UiKit.Tip("Clearance kept from the AOE edge.");

                Key("Clearance lead");
                var lead = cfg.AutoDodgeClearanceLead;
                if (ImGui.SliderFloat("##lead", ref lead, 0f, 5f, "%.1f s"))
                {
                    cfg.AutoDodgeClearanceLead = Math.Clamp(lead, 0f, 5f);
                    changed = true;
                }
                UiKit.Tip("Be clear this early. Costs uptime, buys margin.");

                ImGui.EndTable();
            }
            if (cfg.AutoDodgeSafetyMargin <= 0f)
                ImGui.TextColored(UiKit.Amber, "! at 0y you may still get hit at times");
            if (cfg.AutoDodgeClearanceLead <= 0f)
                ImGui.TextColored(UiKit.Amber, "! at 0s you leave exactly as the AOE lands");
            UiKit.EndCard();

            ImGui.TableNextColumn();
            UiKit.BeginCard("Positionals");
            // A set rather than a choice: with no free hand to hold an override key, the acceptable sides
            // have to be stated up front, and most melee are happy with rear OR flank.
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Sides");
            UiKit.Tip("Acceptable positionals. None ticked = anywhere. A tiebreak only, never worth a death.");
            foreach (var (label, flag) in new[] { ("Front", Positional.Front), ("Flank", Positional.Flank), ("Rear", Positional.Rear) })
            {
                ImGui.SameLine();
                var on = (cfg.DesiredPositional & flag) != 0;
                if (UiKit.Toggle(label, on))
                {
                    cfg.DesiredPositional = on ? cfg.DesiredPositional & ~flag : cfg.DesiredPositional | flag;
                    changed = true;
                }
            }

            // BMR aims for the centre of the arc, so a flank-to-rear switch is 45 degrees of travel. Standing
            // just inside the border makes it 15 -- which matters for jobs with only an oGCD between the two.
            if (ImGui.BeginTable("##pos", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("k", ImGuiTableColumnFlags.WidthFixed, 110f);
                ImGui.TableSetupColumn("v", ImGuiTableColumnFlags.WidthStretch);
                Key("Inside the arc");
                var arc = cfg.PositionalArcMarginDeg;
                if (ImGui.SliderFloat("##arc", ref arc, 0f, 44f, "%.0f deg"))
                {
                    cfg.PositionalArcMarginDeg = Math.Clamp(arc, 0f, 44f);
                    changed = true;
                }
                UiKit.Tip("Stand this far inside the positional arc. 0 = dead centre, as BossmodReborn does it; more = a shorter turn when the next hit wants the neighbouring side.");
                ImGui.EndTable();
            }
            ImGui.TextDisabled("None ticked = anywhere. A tiebreak only, never worth a death.");
            UiKit.EndCard();

            ImGui.EndTable();
        }

        UiKit.BeginCard("Preset", cfg.ActivePreset);
        this.DrawPresets(cfg);
        UiKit.EndCard();

        UiKit.BeginCard("Windows");
        if (ImGui.Button("Radar")) this.plugin.ShowRadar();
        ImGui.SameLine();
        if (ImGui.Button("Strategies")) this.plugin.ToggleEncounterSettings();
        UiKit.Tip("Per-fight strategy settings.");
        ImGui.SameLine();
        if (ImGui.Button("Debug")) this.plugin.ToggleDebug();
        ImGui.SameLine();
        if (ImGui.Button("Sandbox")) this.plugin.ToggleSandbox();
        ImGui.SameLine();
        ImGui.TextDisabled("/mine · radar · modules · record · strats · debug · sandbox · face <dir>");
        UiKit.EndCard();

        if (changed)
            cfg.Save();
    }

    /// <summary>
    /// Preset row: pick one, save the current settings as a new one, or delete a saved one.
    /// <para>It also shows when another plugin holds the slot, and when the live settings have drifted
    /// from the preset they came from. Both are states a user would otherwise read as Minerva having
    /// forgotten their configuration.</para>
    /// </summary>
    private void DrawPresets(Configuration cfg)
    {
        var presets = this.plugin.Presets;
        var all = presets.All();
        var names = new string[all.Count];
        var current = 0;
        for (var i = 0; i < all.Count; ++i)
        {
            names[i] = all[i].Name;
            if (string.Equals(all[i].Name, cfg.ActivePreset, StringComparison.OrdinalIgnoreCase))
                current = i;
        }

        ImGui.SetNextItemWidth(150f);
        if (ImGui.Combo("##preset", ref current, names, names.Length))
            presets.Apply(names[current]);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(140f);
        ImGui.InputTextWithHint("##presetname", "new preset name", ref this.presetName, 48);
        ImGui.SameLine();
        if (ImGui.Button("Save as") && presets.Save(this.presetName))
            this.presetName = string.Empty;

        var isBuiltIn = string.Equals(cfg.ActivePreset, DodgePresets.DefaultName, StringComparison.OrdinalIgnoreCase);
        ImGui.SameLine();
        if (isBuiltIn)
        {
            ImGui.BeginDisabled();
            ImGui.Button("Delete");
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Default is the fallback every other preset returns to, so it stays.");
        }
        else if (ImGui.Button("Delete"))
        {
            presets.Delete(cfg.ActivePreset);
        }

        if (presets.Owner is { } owner)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.72f, 0.51f, 0.94f, 1f), $"held by {owner}");
        }
        else if (presets.Modified)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(edited - save to keep)");
        }
    }

    /// <summary>Checkboxes in two columns inside a card.</summary>
    private static bool Grid2(params (string Label, bool Value, Action<bool> Set, string? Help)[] items)
    {
        var changed = false;
        if (!ImGui.BeginTable("##g2", 2, ImGuiTableFlags.SizingStretchSame))
            return false;
        foreach (var (label, value, set, help) in items)
        {
            ImGui.TableNextColumn();
            var v = value;
            if (ImGui.Checkbox(label, ref v))
            {
                set(v);
                changed = true;
            }
            UiKit.Tip(help);
        }
        ImGui.EndTable();
        return changed;
    }

    /// <summary>Label cell, then move to the value cell with the control set to fill it.</summary>
    private static void Key(string label)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-float.Epsilon);
    }

    public void Dispose()
    {
    }
}
