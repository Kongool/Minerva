using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Minerva.Ui;

namespace Minerva.Windows;

/// <summary>
/// The edge tabs: tabs pinned to a screen edge with sideways labels, each popping its panel out beside it while
/// pointed at (click to pin). Ported from Daedalus's <c>EdgeDockWindow</c>, and laid out with its dock and Charon's
/// through the same shared list (<see cref="DockLayoutShare"/>), so tabs on one edge stack instead of overlapping.
/// <para>A tab's panel is the BODY of the window it stands for — that window's own <c>Draw()</c>, called inside the
/// pop-out — so the tab and the window cannot drift apart.</para>
/// <para>Unlike Daedalus's panels, Minerva's have a fixed size: the radar and the main window both fill whatever
/// space their window has (the radar sizes the field off it, the main window's page is a fill-the-rest child), and
/// in a window that sizes itself to its content those chase each other to nothing.</para>
/// <para>The sideways label is ordinary text whose vertices are turned after it is drawn (<see cref="DockMath"/>).
/// It is drawn under a full-screen clip rect: ImGui culls glyphs against the clip rect BEFORE they are turned, and
/// the unturned text runs far outside a 34-pixel tab.</para>
/// </summary>
public sealed unsafe class EdgeDockWindow : Window
{
    /// <param name="Id">Stable id; also the ImGui id suffix.</param>
    /// <param name="Label">What the sideways tab reads.</param>
    /// <param name="Applies">Whether the tab is shown right now.</param>
    /// <param name="DrawPanel">The panel body — the target window's own <c>Draw()</c>.</param>
    /// <param name="PanelSize">The panel's size, fixed both ways.</param>
    /// <param name="Style">Pushes the style the body expects (its window's theme) before the panel begins; disposing
    /// pops it after the panel ends, so the panel's own background takes it too.</param>
    public sealed record Tab(string Id, string Label, Func<bool> Applies, Action DrawPanel, Vector2 PanelSize, Func<IDisposable>? Style = null);

    private const float TabWidth = 34f;
    private const float TabPadding = 14f;
    private const float TabGap = 4f;
    private const float PanelGap = 6f;
    private const float LabelScale = 1.25f;
    private static readonly TimeSpan CloseDelay = TimeSpan.FromMilliseconds(450);

    private readonly IReadOnlyList<Tab> tabs;
    private readonly Func<bool> rightSide;
    private readonly DockLayoutShare? layout;
    private readonly string owner;
    private readonly int order;
    private readonly DockHover hover = new();
    private List<Tab> visible = [];

    /// <param name="owner">This dock's name in the shared layout, unique across plugins ("Minerva", "Minerva Radar").</param>
    /// <param name="order">Where it sits among the docks on its edge, smallest at the top.</param>
    /// <param name="layout">The layout shared with other plugins' docks. Null centres this dock on its own.</param>
    public EdgeDockWindow(string owner, int order, IReadOnlyList<Tab> tabs, Func<bool> rightSide, DockLayoutShare? layout = null)
        : base($"##MinervaEdgeDock{owner}")
    {
        this.owner = owner;
        this.order = order;
        this.tabs = tabs;
        this.rightSide = rightSide;
        this.layout = layout;

        this.Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar;
        this.RespectCloseHotkey = false;
        this.PositionCondition = ImGuiCond.Always;
        this.IsOpen = false;
    }

    /// <summary>Whether any tab applies right now — the caller opens the window only then.</summary>
    public bool AnyTab()
    {
        this.visible = this.tabs.Where(t => t.Applies()).ToList();
        return this.visible.Count > 0;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        var viewport = ImGui.GetMainViewport();
        var total = this.visible.Sum(t => TabHeight(t.Label)) + TabGap * Math.Max(0, this.visible.Count - 1);
        var right = this.rightSide();
        var x = right ? viewport.WorkPos.X + viewport.WorkSize.X - TabWidth : viewport.WorkPos.X;
        var y = this.layout?.Place(this.owner, this.order, right, total, viewport.WorkPos.Y, viewport.WorkSize.Y)
            ?? viewport.WorkPos.Y + (viewport.WorkSize.Y - total) / 2f;
        this.Position = new Vector2(x, y);
    }

    public override void PostDraw() => ImGui.PopStyleVar();

    public override void Draw()
    {
        var drawList = ImGui.GetWindowDrawList();
        var right = this.rightSide();
        string? hovered = null;
        Tab? openTab = null;
        var openMin = Vector2.Zero;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, TabGap));
        foreach (var tab in this.visible)
        {
            var size = new Vector2(TabWidth, TabHeight(tab.Label));
            var min = ImGui.GetCursorScreenPos();

            if (ImGui.InvisibleButton($"##dock{tab.Id}", size))
                this.hover.Click(tab.Id);
            if (ImGui.IsItemHovered())
                hovered = tab.Id;

            var isOpen = this.hover.Open == tab.Id;
            DrawTab(drawList, min, size, tab.Label, isOpen || hovered == tab.Id, this.hover.Pinned == tab.Id, right);
            if (isOpen)
            {
                openTab = tab;
                openMin = min;
            }
        }
        ImGui.PopStyleVar();

        var panelBusy = openTab != null && DrawPanel(openTab, openMin, right);
        this.hover.Update(hovered, panelBusy, DateTime.UtcNow, CloseDelay, this.visible.Select(t => t.Id).ToList());
    }

    private static float TabHeight(string label) => MathF.Max(90f, ImGui.CalcTextSize(label).X * LabelScale + TabPadding * 2f);

    private static void DrawTab(ImDrawListPtr drawList, Vector2 min, Vector2 size, string label, bool lit, bool pinned, bool right)
    {
        var max = min + size;
        var fill = lit ? AegisTheme.BasaltRaised with { W = 0.96f } : AegisTheme.Basalt with { W = 0.88f };
        // rounded on the side facing the screen, square against the edge it is pinned to
        var corners = right ? ImDrawFlags.RoundCornersLeft : ImDrawFlags.RoundCornersRight;
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(fill), 6f, corners);
        drawList.AddRect(min, max, ImGui.GetColorU32(AegisTheme.Rule), 6f, corners);

        if (pinned)
        {
            // bronze on the tab's outer edge marks a pinned panel
            var barMin = right ? new Vector2(min.X, min.Y) : new Vector2(max.X - 2f, min.Y);
            drawList.AddRectFilled(barMin, barMin + new Vector2(2f, size.Y), ImGui.GetColorU32(AegisTheme.BronzeBright));
        }

        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize() * LabelScale;
        var textSize = ImGui.CalcTextSize(label) * LabelScale;

        // turned, the label is textSize.Y wide and textSize.X tall -- centred in the tab
        var topLeft = min + new Vector2((size.X - textSize.Y) / 2f, (size.Y - textSize.X) / 2f);
        var pivot = DockMath.PivotFor(topLeft, textSize);
        var color = ImGui.GetColorU32(lit ? AegisTheme.BronzeBright : AegisTheme.TravertineDim);

        // drawn first near the top-left of the screen, where the whole unturned line fits, then turned and moved in
        var drawnAt = ImGui.GetMainViewport().WorkPos + new Vector2(8f, 8f);
        drawList.PushClipRectFullScreen();
        var first = drawList.VtxBuffer.Size;
        drawList.AddText(font, fontSize, drawnAt, color, label);
        var last = drawList.VtxBuffer.Size;
        var vertices = drawList.VtxBuffer.Data;
        for (var i = first; i < last; i++)
            vertices[i].Pos = DockMath.TurnUpAndPlace(vertices[i].Pos, drawnAt, pivot);
        drawList.PopClipRect();
    }

    /// <summary>Draws the open tab's panel beside it. True while the pointer is on it or it is mid-interaction.</summary>
    private static bool DrawPanel(Tab tab, Vector2 tabMin, bool right)
    {
        var viewport = ImGui.GetMainViewport();
        var x = right ? tabMin.X - PanelGap : tabMin.X + TabWidth + PanelGap;
        // level with the tab, pulled up if it would run off the bottom of the screen
        var bottom = viewport.WorkPos.Y + viewport.WorkSize.Y - tab.PanelSize.Y - 4f;
        var y = MathF.Max(viewport.WorkPos.Y + 4f, MathF.Min(tabMin.Y, bottom));

        ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always, new Vector2(right ? 1f : 0f, 0f));
        ImGui.SetNextWindowSize(tab.PanelSize, ImGuiCond.Always);

        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        var busy = false;
        using var style = tab.Style?.Invoke();
        if (ImGui.Begin($"##MinervaEdgeDockPanel{tab.Id}", flags))
        {
            try
            {
                tab.DrawPanel();
            }
            catch (Exception ex)
            {
                // caught HERE: an exception leaving between Begin and End would skip the End and corrupt ImGui's
                // window stack for every plugin drawing after this one
                ImGui.TextColored(UiKit.Red, $"This panel could not be drawn: {ex.Message}");
            }

            // the panel also counts as "in use" while one of its own pop-ups is open or a control is held: a combo's
            // list is a separate window, and losing the panel under it closes the combo
            busy = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows
                                         | ImGuiHoveredFlags.AllowWhenBlockedByPopup
                                         | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)
                   || ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopupId | ImGuiPopupFlags.AnyPopupLevel)
                   || ImGui.IsAnyItemActive();
        }
        ImGui.End();
        return busy;
    }
}

/// <summary>
/// The shared list of docks, held in Dalamud's data share so every plugin's dock sees the same one.
/// <para>Only framework types cross the boundary — a <c>ConcurrentDictionary&lt;string, long[]&gt;</c> keyed by
/// owner, each value <c>[edge, height, order, seenMs]</c> — because the data share hands every plugin the SAME object
/// and a type defined in one plugin's assembly would not match another's. If the share cannot be had, the dock centres
/// on its own.</para>
/// <para>One per plugin, however many docks it has: Minerva publishes its radar's and its main window's as two owners,
/// so they can sit on different edges and still stack like anyone else's when they share one.</para>
/// </summary>
public sealed class DockLayoutShare : IDisposable
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ConcurrentDictionary<string, long[]>? slots;
    private readonly HashSet<string> published = [];

    public DockLayoutShare(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        try
        {
            this.slots = pluginInterface.GetOrCreateData(DockLayout.ShareTag, () => new ConcurrentDictionary<string, long[]>());
        }
        catch
        {
            this.slots = null;
        }
    }

    /// <summary>Publish this frame's dock and get back where its top goes.</summary>
    public float Place(string owner, int order, bool rightEdge, float height, float workTop, float workHeight)
    {
        var edge = rightEdge ? 1 : 0;
        if (this.slots is null)
            return workTop + MathF.Max(0f, (workHeight - height) / 2f);

        var now = Environment.TickCount64;
        this.slots[owner] = [edge, (long)MathF.Ceiling(height), order, now];
        this.published.Add(owner);
        return DockLayout.TopFor(owner, edge, height, Read(this.slots), now, workTop, workHeight);
    }

    private static IEnumerable<DockSlot> Read(ConcurrentDictionary<string, long[]> slots)
    {
        foreach (var (owner, v) in slots)
            if (v is { Length: >= 4 })
                yield return new DockSlot(owner, (int)v[0], v[1], (int)v[2], v[3]);
    }

    public void Dispose()
    {
        if (this.slots is null)
            return;
        foreach (var owner in this.published)
            this.slots.TryRemove(owner, out _);
        this.pluginInterface.RelinquishData(DockLayout.ShareTag);
    }
}

/// <summary>A theme pushed for one panel, popped when disposed; with any extra colours pushed after it.</summary>
public sealed class ThemeScope((int Colors, int Vars) theme, int extraColors = 0) : IDisposable
{
    public void Dispose()
    {
        if (extraColors > 0)
            ImGui.PopStyleColor(extraColors);
        AegisTheme.Pop(theme);
    }
}
