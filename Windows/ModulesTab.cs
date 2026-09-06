using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Lumina.Excel.Sheets;
using Minerva.Modules;

namespace Minerva.Windows;

/// <summary>
/// The Modules page: every boss module Minerva knows, grouped by duty and filtered by expansion and content
/// type — icon toggles across the top, a search box, collapsible duty groups.
/// <para>The list is built entirely from game data: a module declares only its duty (<c>CFCID</c>) and its
/// boss (<c>NameID</c>), and the duty name, its icon, its content type and its expansion all come from the
/// sheets. Nothing has to be annotated by hand, so a newly ported module shows up correctly the moment it
/// is registered.</para>
/// <para>The filters hide entries; they never disable anything. A radar that silently stopped working
/// because a tickbox was cleared weeks ago would be a poor trade for a shorter list.</para>
/// </summary>
public sealed class ModulesTab
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

    private sealed record Entry(string Boss, ModuleMaturity Maturity, uint PrimaryOID, string TypeName, ModuleGroup Family);

    private sealed record Group(uint CFCID, string Duty, uint Icon, int Expansion, int Category, uint Sort, List<Entry> Entries, ModuleGroup Family);

    public ModulesTab(ModuleManager modules) => this.modules = modules;

    /// <summary>The name a boss module goes by: its BNpcName, else its class name spaced out.</summary>
    public static string BossName(ModuleRegistry.Info info)
        => (info.Attr.Group is ModuleGroup.CriticalEngagement or ModuleGroup.BozjaDuel ? ResolveEventName(info.Attr.NameID) : ResolveBossName(info.Attr.NameID))
            ?? Prettify(info.ModuleType.Name);

    // a critical engagement's NameID is a DynamicEvent row, not a BNpcName (BossmodReborn's convention), which is
    // why "CE211LostontheWind" was being spelled out from its class name
    internal static string? ResolveEventName(uint eventId)
    {
        if (eventId == 0)
            return null;
        try
        {
            var sheet = Service.DataManager.GetExcelSheet<DynamicEvent>();
            if (sheet != null && sheet.TryGetRow(eventId, out var row))
            {
                var n = row.Name.ExtractText();
                return string.IsNullOrWhiteSpace(n) ? null : n;
            }
        }
        catch { /* sheet/layout mismatch -- fall back to the class name */ }
        return null;
    }

    /// <summary>
    /// Heading for each content family, in the order they are worth browsing.
    /// <para>Occult Crescent is the reason this exists: every critical engagement and every FATE in a horn
    /// reports the same ContentFinderCondition — the zone — so keying the list on duty alone piles thirty
    /// unrelated fights under one heading. BossmodReborn separates them and so should this.</para>
    /// </summary>
    private static string FamilyName(ModuleGroup g) => g switch
    {
        ModuleGroup.CriticalEngagement => "Critical Engagements",
        ModuleGroup.ForayFATE => "Foray FATEs",
        ModuleGroup.Fate => "FATEs",
        ModuleGroup.Hunt => "Hunts",
        ModuleGroup.Quest => "Quest Battles",
        ModuleGroup.TheForkedTowerBlood => "The Forked Tower (Blood)",
        ModuleGroup.TheForkedTowerMagic => "The Forked Tower (Magic)",
        ModuleGroup.BaldesionArsenal => "Baldesion Arsenal",
        ModuleGroup.CastrumLacusLitore => "Castrum Lacus Litore",
        ModuleGroup.TheDalriada => "The Dalriada",
        ModuleGroup.BozjaDuel => "Bozja Duels",
        ModuleGroup.EurekaNM => "Eureka Notorious Monsters",
        ModuleGroup.MaskedCarnivale => "Masked Carnivale",
        ModuleGroup.GoldSaucer => "Gold Saucer",
        _ => "Duties",
    };

    private static int FamilyOrder(ModuleGroup g) => g switch
    {
        ModuleGroup.CFC => 0,
        ModuleGroup.CriticalEngagement => 1,
        ModuleGroup.ForayFATE => 2,
        ModuleGroup.TheForkedTowerBlood => 3,
        ModuleGroup.TheForkedTowerMagic => 4,
        ModuleGroup.Fate => 5,
        ModuleGroup.Hunt => 6,
        _ => 7,
    };

    public void Draw()
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

                // an unloaded icon still has to toggle, so the fallback is a blank button of the same
                // size rather than a spacer -- the tooltip below names it either way
                var clicked = tex.Handle != 0
                    ? ImGui.ImageButton(tex.Handle, FilterIconSize, Vector2.Zero, Vector2.One, 1, Vector4.Zero, tint)
                    : ImGui.Button(string.Empty, FilterIconSize);
                if (clicked)
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
            if (tex.Handle != 0)
                ImGui.Image(tex.Handle, GroupIconSize);
            else
                ImGui.Dummy(GroupIconSize); // keep the header text aligned when the icon hasn't loaded
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();

            // a search that matched should show the hit, not make you open every group to find it
            if (this.search.Length != 0)
                ImGui.SetNextItemOpen(true, ImGuiCond.Always);
            if (ImGui.CollapsingHeader($"{g.Duty}  ({matching.Count})##{g.CFCID}"))
            {
                ImGui.Indent(GroupIconSize.X);
                foreach (var e in matching)
                {
                    var isActive = active == e.TypeName;
                    ImGui.TextColored(isActive ? UiKit.Green : AegisTheme.Travertine, e.Boss);
                    ImGui.SameLine();
                    ImGui.TextDisabled($"0x{e.PrimaryOID:X}");
                    ImGui.SameLine();
                    if (isActive)
                        ImGui.TextColored(UiKit.Green, "· active now");
                    else if (e.Maturity == ModuleMaturity.WIP)
                        ImGui.TextColored(UiKit.Amber, "· WIP");
                    else
                        ImGui.TextColored(UiKit.Green, $"· {e.Maturity}");
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
        var typeSheet = Service.DataManager.GetExcelSheet<ContentType>();
        var exSheet = Service.DataManager.GetExcelSheet<ExVersion>();

        var expansionIndex = new Dictionary<int, int>();
        var categoryIndex = new Dictionary<uint, int>();

        try
        {
            foreach (var (cfcID, infos) in this.modules.ModulesByCFC)
            {
                // CFC 0 is not a duty: hunts, FATEs and quest battles happen in the open world
                var duty = cfcID == 0 ? "Open world" : $"Duty {cfcID}";
                uint icon = FallbackIcon, sort = cfcID;
                int expansion = -1, category = -1;

                // Guarded per row: a duty with a dangling reference must not take the whole list down. Row 0
                // has no territory, and a dereference of an invalid RowRef throws rather than returning null.
                try
                {
                    if (cfcID != 0 && cfcSheet != null && cfcSheet.TryGetRow(cfcID, out var cfc))
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

                        if (cfc.TerritoryType.IsValid)
                        {
                            var exVersion = (int)cfc.TerritoryType.Value.ExVersion.RowId;
                            if (!expansionIndex.TryGetValue(exVersion, out expansion))
                            {
                                expansion = this.expansions.Count;
                                expansionIndex[exVersion] = expansion;
                                var exName = exSheet != null && exSheet.TryGetRow((uint)exVersion, out var exRow) ? exRow.Name.ExtractText() : string.Empty;
                                this.expansions.Add((string.IsNullOrWhiteSpace(exName) ? $"Expansion {exVersion}" : exName,
                                    exVersion >= 0 && exVersion < ExpansionIcons.Length ? ExpansionIcons[exVersion] : FallbackIcon));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Service.Log.Warning(ex, $"Minerva: could not describe duty {cfcID} for the module list; listing it plainly.");
                }

                var entries = new List<Entry>();
                foreach (var info in infos)
                    entries.Add(new Entry(BossName(info), info.Attr.Maturity, info.PrimaryActorOID, info.ModuleType.Name, info.Attr.Group));

                entries.Sort((a, b) => string.CompareOrdinal(a.Boss, b.Boss));
                // One CFC can hold several families at once — an Occult Crescent horn reports the same duty
                // for its critical engagements and its FATEs — so split rather than lump.
                foreach (var family in entries.Select(e => e.Family).Distinct())
                {
                    var forFamily = entries.FindAll(e => e.Family == family);
                    var heading = family == ModuleGroup.CFC ? duty : $"{duty} — {FamilyName(family)}";
                    this.groups.Add(new Group(cfcID, heading, icon, expansion, category, sort, forFamily, family));
                }
            }
        }
        finally
        {
            // Sized whatever happened above: the filter toggles index these by the lists they mirror, and an
            // unsized array is how a one-off build failure became an error on every frame after it.
            this.groups.Sort((a, b) =>
            {
                // families first, so a zone's duties, its critical engagements and its FATEs read as separate lists
                var fa = FamilyOrder(a.Family).CompareTo(FamilyOrder(b.Family));
                if (fa != 0)
                    return fa;
                return a.Sort != b.Sort ? a.Sort.CompareTo(b.Sort) : string.CompareOrdinal(a.Duty, b.Duty);
            });
            this.expansionShown = Enumerable.Repeat(true, this.expansions.Count).ToArray();
            this.categoryShown = Enumerable.Repeat(true, this.categories.Count).ToArray();
        }
    }

    // boss name from the BNpcName sheet (module NameID); title-cased since the sheet stores it lowercase.
    // CE modules carry a small non-BNpcName id convention (e.g. 35) — only real BNpcName rows (large ids)
    // resolve; everything else falls back to the prettified class name.
    internal static string? ResolveBossName(uint nameId)
    {
        if (nameId < 1000)
            return null;
        try
        {
            var sheet = Service.DataManager.GetExcelSheet<BNpcName>();
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
    internal static string Prettify(string typeName)
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
}
