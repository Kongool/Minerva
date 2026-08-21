using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Minerva;
using Minerva.Automation;
using Minerva.GameSync;
using Minerva.Modules;
using Minerva.Radar;

namespace Minerva.Windows;

/// <summary>
/// The radar: draws the active module's arena (danger zones, boundary, actors) and its hints. When
/// no module is active it shows a short status line. This is the player-facing payoff of Phases 1–3.
/// </summary>
public sealed class RadarWindow : Window, IDisposable
{
    private readonly ModuleManager manager;
    private readonly AIManager ai;
    private readonly Configuration config;
    private readonly WorldStateDebugWindow debug;
    private readonly MainWindow menu;
    private readonly ReplayWindow replay;
    private readonly ImGuiArena arena = new();

    public RadarWindow(ModuleManager manager, AIManager ai, Configuration config, WorldStateDebugWindow debug, MainWindow menu, ReplayWindow replay)
        : base("Minerva Radar###MinervaRadar")
    {
        this.manager = manager;
        this.ai = ai;
        this.config = config;
        this.debug = debug;
        this.menu = menu;
        this.replay = replay;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 360),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    private (int Colors, int Vars) theme;

    public override void PreDraw() => this.theme = AegisTheme.Push();

    public override void PostDraw() => AegisTheme.Pop(this.theme);


    public override void Draw()
    {
        if (!ImGui.BeginTabBar("###radartabs"))
            return;

        this.Tab("Radar", this.DrawRadar);
        this.Tab("Menu", this.menu.DrawContent);
        this.Tab("Modules", this.DrawModules);
        this.Tab("Replay", this.replay.DrawContent);
        this.Tab("Debug", this.debug.DrawContent);

        ImGui.EndTabBar();
    }

    private string moduleSearch = string.Empty;
    private string selectedCat = string.Empty; // "" = all, "E:<expansion>", or "C:<content>"

    /// <summary>
    /// A searchable, categorised list of every registered module (a take on BMR's Supported Fights): a
    /// left sidebar filters by Expansion / Content-type, the right pane groups the matches by duty (CFC)
    /// in collapsible sections, each row showing the real boss name (NameID→BNpcName, else the class name),
    /// the boss OID, and maturity.
    /// </summary>
    private void DrawModules()
    {
        var all = this.manager.ModulesByCFC.Values.SelectMany(l => l).ToList();
        var expansions = all.Select(i => Categorize(i).expansion).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
        var contents = all.Select(i => Categorize(i).content).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();

        // --- sidebar: category selection ---
        ImGui.BeginChild("##modsidebar", new Vector2(150f, 0f), true);
        if (ImGui.Selectable($"All ({all.Count})", this.selectedCat.Length == 0))
            this.selectedCat = string.Empty;
        ImGui.Spacing();
        ImGui.TextDisabled("Expansion");
        foreach (var e in expansions)
            if (ImGui.Selectable(e, this.selectedCat == "E:" + e))
                this.selectedCat = "E:" + e;
        ImGui.Spacing();
        ImGui.TextDisabled("Content");
        foreach (var c in contents)
            if (ImGui.Selectable(c, this.selectedCat == "C:" + c))
                this.selectedCat = "C:" + c;
        ImGui.EndChild();

        ImGui.SameLine();

        // --- right pane: search + grouped list ---
        ImGui.BeginChild("##modlist", new Vector2(0f, 0f), true);
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##modsearch", "Search modules…", ref this.moduleSearch, 64);
        ImGui.Separator();

        var filter = this.moduleSearch.Trim();
        foreach (var (cfc, list) in this.manager.ModulesByCFC.OrderBy(kv => kv.Key))
        {
            var duty = ResolveDutyName(cfc);
            var rows = list
                .Where(this.MatchesCategory)
                .Select(i => (info: i, name: ResolveBossName(i.Attr.NameID) ?? Prettify(i.ModuleType.Name)))
                .Where(x => filter.Length == 0
                    || x.name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || (duty?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderBy(x => x.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (rows.Count == 0)
                continue;

            var header = duty != null ? $"{duty}  (CFC {cfc}) — {rows.Count}" : $"CFC {cfc} — {rows.Count}";
            if (!ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
                continue;

            foreach (var (info, name) in rows)
            {
                DrawDutyIcon(cfc);
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(name);
                ImGui.SameLine();
                ImGui.TextDisabled($" 0x{info.PrimaryActorOID:X}");
                ImGui.SameLine();
                var wip = info.Attr.Maturity == ModuleMaturity.WIP;
                ImGui.TextColored(wip ? new Vector4(1f, 0.7f, 0.2f, 1f) : new Vector4(0.3f, 1f, 0.3f, 1f), wip ? " · WIP" : " · Verified");
            }
        }
        ImGui.EndChild();
    }

    private bool MatchesCategory(ModuleRegistry.Info info)
    {
        if (this.selectedCat.Length == 0)
            return true;
        var (e, c) = Categorize(info);
        return this.selectedCat == "E:" + e || this.selectedCat == "C:" + c;
    }

    // Expansion + content-type from the module's namespace/name: Minerva.<Expansion>.<Kind>.<Module>.
    // Foray splits into "Critical Engagement" (CE### classes) vs "Field Boss"; Dungeon stays "Dungeon".
    private static (string expansion, string content) Categorize(ModuleRegistry.Info info)
    {
        var ns = info.ModuleType.Namespace ?? string.Empty;
        var parts = ns.Split('.');
        var expansion = parts.Length > 1 ? parts[1] : "Other";
        var name = info.ModuleType.Name;
        var isCE = name.Length > 2 && name[0] == 'C' && name[1] == 'E' && char.IsDigit(name[2]);
        var content = ns.Contains(".Dungeon") ? "Dungeon"
            : isCE ? "Critical Engagement"
            : ns.Contains(".Foray") ? "Field Boss"
            : parts.Length > 2 ? parts[2] : "Other";
        return (expansion, content);
    }

    // boss name from the BNpcName sheet (module NameID); title-cased since the sheet stores it lowercase.
    // CE modules carry a small non-BNpcName id convention (e.g. 35) — only real BNpcName rows (large ids)
    // resolve; everything else falls back to the prettified class name.
    private static string? ResolveBossName(uint nameId)
    {
        if (nameId < 1000)
            return null;
        try
        {
            var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.BNpcName>();
            if (sheet != null && sheet.TryGetRow(nameId, out var row))
            {
                var n = row.Singular.ExtractText();
                return string.IsNullOrWhiteSpace(n) ? null : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(n);
            }
        }
        catch { /* sheet/layout mismatch — fall back to the class name */ }
        return null;
    }

    // "DemiMedusa" -> "Demi Medusa", "CE107Unbridled" -> "CE107 Unbridled" (space before an uppercase that follows a non-uppercase)
    private static string Prettify(string typeName)
    {
        var sb = new System.Text.StringBuilder(typeName.Length + 6);
        for (var i = 0; i < typeName.Length; ++i)
        {
            if (i > 0 && char.IsUpper(typeName[i]) && !char.IsUpper(typeName[i - 1]))
                sb.Append(' ');
            sb.Append(typeName[i]);
        }
        return sb.ToString();
    }

    // the duty's banner from ContentFinderCondition, drawn small before each row (shared within a duty since
    // the game has no per-boss icon). Reserves a same-size blank when the duty has no banner (field ops), so
    // rows stay aligned.
    private static void DrawDutyIcon(uint cfc)
    {
        var size = ImGui.GetFrameHeight();
        var box = new Vector2(size, size);
        var id = DutyImageId(cfc);
        if (id != 0)
        {
            try
            {
                var tex = Service.TextureProvider.GetFromGameIcon(new GameIconLookup(id)).GetWrapOrEmpty();
                if (tex.Handle != 0 && tex.Size.Y > 0f)
                {
                    ImGui.Image(tex.Handle, new Vector2(size * tex.Size.X / tex.Size.Y, size));
                    return;
                }
            }
            catch { /* texture unavailable — fall through to the spacer */ }
        }
        ImGui.Dummy(box);
    }

    private static uint DutyImageId(uint cfc)
    {
        try
        {
            var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>();
            if (sheet != null && sheet.TryGetRow(cfc, out var row))
                return row.Image;
        }
        catch { /* sheet/layout mismatch */ }
        return 0;
    }

    // duty/encounter name from the Content Finder Condition sheet; null if it has no display name
    private static string? ResolveDutyName(uint cfc)
    {
        try
        {
            var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ContentFinderCondition>();
            if (sheet != null && sheet.TryGetRow(cfc, out var row))
            {
                var n = row.Name.ExtractText();
                return string.IsNullOrWhiteSpace(n) ? null : n;
            }
        }
        catch { /* sheet/layout mismatch — just show the id */ }
        return null;
    }

    /// <summary>
    /// Draw one tab with its content guarded. If the content throws, we still run EndTabItem so ImGui's
    /// tab/ID stack stays balanced — an unbalanced stack is what turns a draw bug into a hard crash that
    /// takes the whole plugin down. The exception is shown in-tab and logged once so it can be fixed.
    /// </summary>
    /// <summary>Programmatically switch to a tab next frame (e.g. from the "Radar" button in the Menu tab).</summary>
    public void SelectTab(string tab) => this.pendingTab = tab;

    private string? pendingTab;

    private void Tab(string label, Action content)
    {
        var flags = ImGuiTabItemFlags.None;
        if (this.pendingTab == label)
        {
            flags = ImGuiTabItemFlags.SetSelected;
            this.pendingTab = null;
        }

        if (!ImGui.BeginTabItem(label, flags))
            return;
        try
        {
            content();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), $"{label} draw error: {ex.Message}");
            Service.Log.Error(ex, $"Minerva: {label} tab draw threw.");
        }
        finally
        {
            ImGui.EndTabItem();
        }
    }

    private void DrawRadar()
    {
        var module = this.manager.ActiveModule;
        if (module == null)
        {
            ImGui.TextUnformatted("No active encounter.");
            ImGui.TextDisabled($"{this.manager.RegisteredCount} module(s) registered. Waiting for a known boss.");
            return;
        }

        var pc = this.manager.LocalPlayer();
        var pcSlot = 0;

        // hints above the arena
        var global = new ModuleComponent.GlobalHints();
        module.AddGlobalHints(global);
        foreach (var h in global)
            ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), h);

        if (pc != null)
        {
            var hints = new ModuleComponent.TextHints();
            module.AddHints(pcSlot, pc, hints);
            foreach (var (text, risk) in hints)
                ImGui.TextColored(risk ? new Vector4(1f, 0.3f, 0.3f, 1f) : new Vector4(0.8f, 0.8f, 0.8f, 1f), text);
        }

        // auto-move diagnostics: makes it obvious why steering may not be happening
        if (this.config.AutoDodgeEnabled && this.ai.Movement is MovementController mc)
        {
            var backend = mc.NavmeshBackend ?? "direct";
            if (mc.Steering)
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), $"Auto-move: steering to safe spot ({backend})");
            else if (!mc.UsingNavmesh && !mc.HookInstalled)
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Auto-move: no navmesh and movement hook NOT installed (signature outdated) — check /xllog");
            else if (this.ai.HasSolution && this.ai.Current.NeedToMove && !this.ai.Current.Found)
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), "Auto-move: in danger but no safe spot found");
            else
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), $"Auto-move: idle ({backend}, no imminent danger)");
        }

        // arena canvas
        var canvasTopLeft = ImGui.GetCursorScreenPos();
        var canvasSize = ImGui.GetContentRegionAvail();
        var side = MathF.Min(canvasSize.X, canvasSize.Y);
        if (side < 32f)
            return;
        canvasSize = new Vector2(side, side);
        ImGui.InvisibleButton("##canvas", canvasSize); // reserve the region

        this.arena.Center = module.Center;
        this.arena.Bounds = module.Bounds;
        // camera-align: put the camera's forward direction at the top of the screen. W2S rotates the world
        // offset by Rotation, so a world direction θ ends up at the top when Rotation = θ - π; the camera's
        // forward is (azimuth + π) in world-rotation terms, which cancels down to just the azimuth.
        this.arena.Rotation = this.config.RadarHeading == RadarHeading.CameraAlign && GameData.TryCameraAzimuth(out var azimuth)
            ? azimuth
            : 0f;
        this.arena.Begin(canvasTopLeft, canvasSize);

        module.Arena = this.arena;

        // draw the local player on top of module content via the foreground pass; module draws the rest
        module.DrawArena(pcSlot, pc ?? module.PrimaryActor);

        // confine danger zones to the field: mask everything past the boundary, then restroke the border
        if (this.config.ClipToArena)
        {
            this.arena.ClipOutsideArena(ImGui.GetColorU32(ImGuiCol.WindowBg) | 0xFF000000u);
            this.arena.DrawBoundary();
        }

        this.arena.DrawCompass();

        if (pc != null)
        {
            this.arena.ActorMarker(pc.Position, pc.Rotation, pc.HitboxRadius, Colors.PC);
            this.DrawDodge(pc);
        }
    }

    // auto-dodge guidance: mark the safe spot and an arrow to it when the player must move
    private void DrawDodge(Actor pc)
    {
        if (!this.config.AutoDodgeGuidance || !this.ai.HasSolution)
            return;
        var s = this.ai.Current;
        if (!s.NeedToMove)
            return;

        if (s.Found)
        {
            this.arena.AddLine(pc.Position, s.Target, Colors.Safe, 3f);
            this.arena.AddCircleFilled(s.Target, 0.8f, Colors.Safe);
            this.arena.AddCircle(s.Target, 0.8f, Colors.PC, 2f);
        }
        else
        {
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "No safe spot!");
        }
    }

    public void Dispose()
    {
    }
}
