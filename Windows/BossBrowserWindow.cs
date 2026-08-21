using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using Minerva.Modules;

namespace Minerva.Windows;

/// <summary>
/// Browse every boss module Minerva knows, grouped by duty and filtered by expansion and content type.
/// <para>The list is built entirely from game data: a module declares only its duty (<c>CFCID</c>) and its
/// boss (<c>NameID</c>), and the duty name, its icon, its content type and its expansion all come from the
/// sheets. Nothing has to be annotated by hand, so a newly ported module shows up correctly the moment it
/// is registered.</para>
/// <para>The filters hide entries; they never disable anything. A radar that silently stopped working
/// because a tickbox was cleared weeks ago would be a poor trade for a shorter list.</para>
/// </summary>
public sealed class BossBrowserWindow : Window
{
    private static readonly Vector2 FilterIconSize = new(28f, 28f);
    private static readonly Vector2 GroupIconSize = new(32f, 32f);

    /// <summary>Expansion icons in release order; indices line up with ExVersion rows.</summary>
    private static readonly uint[] ExpansionIcons = [61875u, 61876u, 61877u, 61878u, 61879u, 61880u, 61881u];
    private const uint FallbackIcon = 61762u;

    private readonly ModuleManager modules;

    private readonly List<Group> groups = [];
    private readonly List<(string Name, uint Icon)> expansions = [];
    private readonly List<(string Name, uint Icon)> categories = [];
    private bool[] expansionShown = [];
    private bool[] categoryShown = [];
    private string search = string.Empty;
    private bool built;

    private sealed record Entry(string Boss, ModuleMaturity Maturity, uint PrimaryOID, string TypeName);

    private sealed record Group(uint CFCID, string Duty, uint Icon, int Expansion, int Category, uint Sort, List<Entry> Entries);

    public BossBrowserWindow(ModuleManager modules) : base("Minerva Bosses###minervabosses")
    {
        this.modules = modules;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(460f, 320f), MaximumSize = new Vector2(1400f, 1200f) };
        this.Size = new Vector2(620f, 560f);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    private (int Colors, int Vars) theme;

    public override void PreDraw() => this.theme = AegisTheme.Push();

    public override void PostDraw() => AegisTheme.Pop(this.theme);


    public override void Draw()
    {
        if (!this.built)
            this.Build();

        this.DrawFilters();
        ImGui.Separator();
        this.DrawGroups();
    }

    private void DrawFilters()
    {
        ImGui.SetNextItemWidth(220f);
        ImGui.InputTextWithHint("##search", "search boss or duty", ref this.search, 64);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            this.search = string.Empty;
        ImGui.SameLine();
        ImGui.TextDisabled($"{this.groups.Sum(g => g.Entries.Count)} modules");

        Toggles(this.expansions, this.expansionShown, "exp");
        Toggles(this.categories, this.categoryShown, "cat");

        static void Toggles(List<(string Name, uint Icon)> items, bool[] shown, string id)
        {
            for (var i = 0; i < items.Count; ++i)
            {
                if (i != 0)
                    ImGui.SameLine();
                var tex = Service.TextureProvider.GetFromGameIcon(new GameIconLookup(items[i].Icon)).GetWrapOrEmpty();

                // dimming reads as "off" at a glance; a checkbox beside an icon does not
                var tint = shown[i] ? Vector4.One : new Vector4(0.32f, 0.32f, 0.32f, 0.6f);
                ImGui.PushID($"{id}{i}");
                if (ImGui.ImageButton(tex.Handle, FilterIconSize, Vector2.Zero, Vector2.One, 1, Vector4.Zero, tint))
                    shown[i] = !shown[i];
                ImGui.PopID();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(items[i].Name);
            }
        }
    }

    private void DrawGroups()
    {
        if (!ImGui.BeginChild("##bosslist"))
        {
            ImGui.EndChild();
            return;
        }

        var active = this.modules.ActiveModuleInfo?.ModuleType.Name;
        var shown = 0;
        foreach (var g in this.groups)
        {
            if (!this.Passes(g, out var matching))
                continue;
            shown++;

            var tex = Service.TextureProvider.GetFromGameIcon(new GameIconLookup(g.Icon)).GetWrapOrEmpty();
            ImGui.Image(tex.Handle, GroupIconSize);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();

            // a search that matched should show the hit, not make you open every group to find it
            if (this.search.Length != 0)
                ImGui.SetNextItemOpen(true, ImGuiCond.Always);
            if (ImGui.CollapsingHeader($"{g.Duty}##{g.CFCID}"))
            {
                ImGui.Indent(GroupIconSize.X);
                foreach (var e in matching)
                {
                    var isActive = active == e.TypeName;
                    if (isActive)
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 1f, 0.5f, 1f));
                    ImGui.TextUnformatted(e.Boss);
                    if (isActive)
                        ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.TextDisabled(isActive ? "(active now)" : e.Maturity.ToString());
                }

                ImGui.Unindent(GroupIconSize.X);
            }
        }

        if (shown == 0)
            ImGui.TextDisabled("Nothing matches those filters.");
        ImGui.EndChild();
    }

    private bool Passes(Group g, out List<Entry> matching)
    {
        matching = g.Entries;
        if (g.Expansion >= 0 && g.Expansion < this.expansionShown.Length && !this.expansionShown[g.Expansion])
            return false;
        if (g.Category >= 0 && g.Category < this.categoryShown.Length && !this.categoryShown[g.Category])
            return false;
        if (this.search.Length == 0)
            return true;

        // a duty match keeps the whole group; otherwise keep only the bosses that matched
        if (g.Duty.Contains(this.search, StringComparison.OrdinalIgnoreCase))
            return true;
        matching = g.Entries.FindAll(e => e.Boss.Contains(this.search, StringComparison.OrdinalIgnoreCase));
        return matching.Count != 0;
    }

    private void Build()
    {
        this.built = true;
        var cfcSheet = Service.DataManager.GetExcelSheet<ContentFinderCondition>();
        var bnpcSheet = Service.DataManager.GetExcelSheet<BNpcName>();
        var typeSheet = Service.DataManager.GetExcelSheet<ContentType>();
        var exSheet = Service.DataManager.GetExcelSheet<ExVersion>();

        var expansionIndex = new Dictionary<int, int>();
        var categoryIndex = new Dictionary<uint, int>();

        foreach (var (cfcID, infos) in this.modules.ModulesByCFC)
        {
            var duty = $"Duty {cfcID}";
            uint icon = FallbackIcon, sort = cfcID;
            int expansion = -1, category = -1;

            if (cfcSheet != null && cfcSheet.TryGetRow(cfcID, out var cfc))
            {
                var name = cfc.Name.ExtractText();
                if (!string.IsNullOrWhiteSpace(name))
                    duty = char.ToUpperInvariant(name[0]) + name[1..];
                sort = cfc.SortKey != 0 ? cfc.SortKey : cfcID;

                var ct = cfc.ContentType.RowId;
                if (ct != 0 && typeSheet != null && typeSheet.TryGetRow(ct, out var ctRow))
                {
                    icon = ctRow.Icon != 0 ? ctRow.Icon : FallbackIcon;
                    if (!categoryIndex.TryGetValue(ct, out category))
                    {
                        category = this.categories.Count;
                        categoryIndex[ct] = category;
                        this.categories.Add((ctRow.Name.ExtractText(), icon));
                    }
                }

                var ex = (int)cfc.TerritoryType.Value.ExVersion.RowId;
                if (!expansionIndex.TryGetValue(ex, out expansion))
                {
                    expansion = this.expansions.Count;
                    expansionIndex[ex] = expansion;
                    var exName = exSheet != null && exSheet.TryGetRow((uint)ex, out var exRow) ? exRow.Name.ExtractText() : string.Empty;
                    this.expansions.Add((string.IsNullOrWhiteSpace(exName) ? $"Expansion {ex}" : exName,
                        ex >= 0 && ex < ExpansionIcons.Length ? ExpansionIcons[ex] : FallbackIcon));
                }
            }

            var entries = new List<Entry>();
            foreach (var info in infos)
            {
                var boss = $"OID {info.PrimaryActorOID:X}";
                if (bnpcSheet != null && info.Attr.NameID != 0 && bnpcSheet.TryGetRow(info.Attr.NameID, out var bn))
                {
                    var n = bn.Singular.ExtractText();
                    if (!string.IsNullOrWhiteSpace(n))
                        boss = char.ToUpperInvariant(n[0]) + n[1..];
                }

                entries.Add(new Entry(boss, info.Attr.Maturity, info.PrimaryActorOID, info.ModuleType.Name));
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.Boss, b.Boss));
            this.groups.Add(new Group(cfcID, duty, icon, expansion, category, sort, entries));
        }

        this.groups.Sort((a, b) => a.Sort != b.Sort ? a.Sort.CompareTo(b.Sort) : string.CompareOrdinal(a.Duty, b.Duty));
        this.expansionShown = Enumerable.Repeat(true, this.expansions.Count).ToArray();
        this.categoryShown = Enumerable.Repeat(true, this.categories.Count).ToArray();
    }
}
