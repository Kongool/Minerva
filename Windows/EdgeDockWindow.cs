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
/// The edge tabs: tabs pinned to the screen edges, each popping its panel out beside it while pointed at (click to
/// pin). Ported from Daedalus's <c>EdgeDockWindow</c>. Every tab has its own edge and its own spot along it; dragging
/// a tab and letting go puts it on the nearest edge, where it was let go. The layout is shared with Daedalus's and
/// Charon's tabs (<see cref="DockLayoutShare"/>), so tabs on one edge are kept apart instead of drawn over each other.
/// <para>This window itself is an invisible 1×1 host. Each tab is its own small ImGui window, so tabs can sit anywhere
/// on any edge.</para>
/// <para>A tab's panel is the BODY of the window it stands for — that window's own <c>Draw()</c>, called inside the
/// pop-out — so the tab and the window cannot drift apart. Unlike Daedalus's panels, Minerva's have a fixed size: the
/// radar and the main window both fill whatever space their window has (the radar sizes the field off it, the main
/// window's page is a fill-the-rest child), and in a window that sizes itself to its content those chase each other to
/// nothing.</para>
/// <para>On the left and right edges the label is ordinary text whose vertices are turned after it is drawn
/// (<see cref="DockMath"/>), under a full-screen clip rect: ImGui culls glyphs against the clip rect BEFORE they are
/// turned, and the unturned text runs far outside a 34-pixel tab. On the top and bottom edges it reads normally.</para>
/// </summary>
public sealed unsafe class EdgeDockWindow : Window
{
    /// <param name="Id">Stable id; also the ImGui id suffix and the layout key.</param>
    /// <param name="Label">What the tab reads.</param>
    /// <param name="Applies">Whether the tab is shown right now.</param>
    /// <param name="DrawPanel">The panel body — the target window's own <c>Draw()</c>.</param>
    /// <param name="PanelSize">The panel's size, fixed both ways.</param>
    /// <param name="Style">Pushes the style the body expects (its window's theme) before the panel begins; disposing
    /// pops it after the panel ends, so the panel's own background takes it too.</param>
    public sealed record Tab(string Id, string Label, Func<bool> Applies, Action DrawPanel, Vector2 PanelSize, Func<IDisposable>? Style = null);

    private const float TabThickness = 34f;
    private const float TabPadding = 14f;
    private const float PanelGap = 6f;
    private const float LabelScale = 1.25f;
    private const float DragThreshold = 6f;
    private static readonly TimeSpan CloseDelay = TimeSpan.FromMilliseconds(450);

    private const ImGuiWindowFlags TabFlags =
        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    private readonly IReadOnlyList<Tab> tabs;
    private readonly Func<string, (DockEdge Edge, float Along)> placement;
    private readonly Action<string, DockEdge, float> moveTab;
    private readonly DockLayoutShare layout;
    private readonly DockHover hover = new();
    private List<Tab> visible = [];
    private string? dragging;

    /// <param name="placement">Where a tab is set to sit: its edge, and its centre as a fraction along it.</param>
    /// <param name="moveTab">A tab was dragged and let go: store its new edge and spot.</param>
    /// <param name="layout">The layout shared with other plugins' tabs, so none cover each other.</param>
    public EdgeDockWindow(IReadOnlyList<Tab> tabs, Func<string, (DockEdge Edge, float Along)> placement, Action<string, DockEdge, float> moveTab, DockLayoutShare layout)
        : base("##MinervaEdgeDock")
    {
        this.tabs = tabs;
        this.placement = placement;
        this.moveTab = moveTab;
        this.layout = layout;

        this.Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs
            | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar;
        this.RespectCloseHotkey = false;
        this.PositionCondition = ImGuiCond.Always;
        this.SizeCondition = ImGuiCond.Always;
        this.Size = Vector2.One;
        this.IsOpen = false;
    }

    /// <summary>Hold a tab's panel out until <see cref="Release"/> (see <see cref="DockHover.Hold"/>).</summary>
    public void Hold(string tabId) => this.hover.Hold(tabId);

    /// <summary>Let a held panel go back to its tab.</summary>
    public void Release(string tabId) => this.hover.Release(tabId);

    /// <summary>Whether any tab applies right now — the caller opens the window only then.</summary>
    public bool AnyTab()
    {
        this.visible = this.tabs.Where(t => t.Applies()).ToList();
        return this.visible.Count > 0;
    }

    public override void PreDraw() => this.Position = ImGui.GetMainViewport().WorkPos;

    public override void Draw()
    {
        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos;
        var workSize = viewport.WorkSize;

        var requests = this.visible.Select(t =>
        {
            var (edge, along) = this.placement(t.Id);
            return new DockRequest(t.Id, edge, along, TabLength(t.Label));
        }).ToList();
        var starts = this.layout.Place(requests, workPos, workSize);

        string? hovered = null;
        Tab? openTab = null;
        var openMin = Vector2.Zero;
        var openSize = Vector2.Zero;
        var openEdge = DockEdge.Left;

        for (var i = 0; i < this.visible.Count; i++)
        {
            var tab = this.visible[i];
            var edge = requests[i].Edge;
            if (!starts.TryGetValue(tab.Id, out var start))
                continue;

            var (min, size) = DockMath.TabRect(edge, start, requests[i].Length, TabThickness, workPos, workSize);
            if (this.DrawTabWindow(tab, edge, min, size, ref hovered))
            {
                openTab = tab;
                openMin = min;
                openSize = size;
                openEdge = edge;
            }
        }

        if (this.dragging != null)
            this.DrawDrag(workPos, workSize);

        var panelBusy = openTab != null && DrawPanel(openTab, openEdge, openMin, openSize);
        this.hover.Update(hovered, panelBusy, DateTime.UtcNow, CloseDelay, this.visible.Select(t => t.Id).ToList());
    }

    /// <summary>One tab, in its own little window. True when its panel is out.</summary>
    private bool DrawTabWindow(Tab tab, DockEdge edge, Vector2 min, Vector2 size, ref string? hovered)
    {
        ImGui.SetNextWindowPos(min, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.One);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

        var isOpen = false;
        if (ImGui.Begin($"##MinervaEdgeDockTab{tab.Id}", TabFlags))
        {
            // a press that moves becomes a drag; only a press that does not is a click (pin / unpin)
            var clicked = ImGui.InvisibleButton($"##dock{tab.Id}", size);
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left, DragThreshold))
                this.dragging = tab.Id;
            if (clicked && this.dragging != tab.Id)
                this.hover.Click(tab.Id);
            if (ImGui.IsItemHovered() && this.dragging == null)
                hovered = tab.Id;

            isOpen = this.hover.Open == tab.Id && this.dragging == null;
            var lit = isOpen || hovered == tab.Id || this.dragging == tab.Id;
            DrawTab(ImGui.GetWindowDrawList(), min, size, tab.Label, lit, this.hover.Pinned == tab.Id, edge);
        }

        ImGui.End();
        ImGui.PopStyleVar(3);
        return isOpen;
    }

    /// <summary>A tab being dragged: the edge it would land on lit bronze, an outline where it would sit, and on
    /// release the move.</summary>
    private void DrawDrag(Vector2 workPos, Vector2 workSize)
    {
        var mouse = ImGui.GetMousePos();
        var target = DockMath.NearestEdge(mouse, workPos, workSize);
        var along = DockMath.AlongFraction(target, mouse, workPos, workSize);

        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            var id = this.dragging!;
            this.dragging = null;
            this.moveTab(id, target, along);
            return;
        }

        var fg = ImGui.GetForegroundDrawList();
        var bronze = ImGui.GetColorU32(AegisTheme.BronzeBright);
        var a = workPos;
        var b = workPos + workSize;
        const float bar = 3f;
        switch (target)
        {
            case DockEdge.Left: fg.AddRectFilled(a, new Vector2(a.X + bar, b.Y), bronze); break;
            case DockEdge.Right: fg.AddRectFilled(new Vector2(b.X - bar, a.Y), b, bronze); break;
            case DockEdge.Top: fg.AddRectFilled(a, new Vector2(b.X, a.Y + bar), bronze); break;
            case DockEdge.Bottom: fg.AddRectFilled(new Vector2(a.X, b.Y - bar), b, bronze); break;
        }

        // the outline of where it would land, before any nudge from its neighbours
        var label = this.visible.FirstOrDefault(t => t.Id == this.dragging)?.Label ?? this.dragging!;
        var length = TabLength(label);
        var horizontal = DockMath.IsHorizontal(target);
        var edgeStart = horizontal ? workPos.X : workPos.Y;
        var edgeLength = horizontal ? workSize.X : workSize.Y;
        var start = Math.Clamp(edgeStart + along * edgeLength - length / 2f, edgeStart, MathF.Max(edgeStart, edgeStart + edgeLength - length));
        var (min, size) = DockMath.TabRect(target, start, length, TabThickness, workPos, workSize);
        fg.AddRect(min, min + size, bronze, 6f, ImDrawFlags.None, 2f);
    }

    private static float TabLength(string label) => MathF.Max(90f, ImGui.CalcTextSize(label).X * LabelScale + TabPadding * 2f);

    private static void DrawTab(ImDrawListPtr drawList, Vector2 min, Vector2 size, string label, bool lit, bool pinned, DockEdge edge)
    {
        var max = min + size;
        var fill = lit ? AegisTheme.BasaltRaised with { W = 0.96f } : AegisTheme.Basalt with { W = 0.88f };
        // rounded on the side facing the screen, square against the edge it is pinned to
        var corners = edge switch
        {
            DockEdge.Right => ImDrawFlags.RoundCornersLeft,
            DockEdge.Top => ImDrawFlags.RoundCornersBottom,
            DockEdge.Bottom => ImDrawFlags.RoundCornersTop,
            _ => ImDrawFlags.RoundCornersRight,
        };
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(fill), 6f, corners);
        drawList.AddRect(min, max, ImGui.GetColorU32(AegisTheme.Rule), 6f, corners);

        if (pinned)
        {
            // bronze on the tab's screen-facing side marks a pinned panel
            var (barMin, barSize) = edge switch
            {
                DockEdge.Right => (min, new Vector2(2f, size.Y)),
                DockEdge.Top => (new Vector2(min.X, max.Y - 2f), new Vector2(size.X, 2f)),
                DockEdge.Bottom => (min, new Vector2(size.X, 2f)),
                _ => (new Vector2(max.X - 2f, min.Y), new Vector2(2f, size.Y)),
            };
            drawList.AddRectFilled(barMin, barMin + barSize, ImGui.GetColorU32(AegisTheme.BronzeBright));
        }

        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize() * LabelScale;
        var textSize = ImGui.CalcTextSize(label) * LabelScale;
        var color = ImGui.GetColorU32(lit ? AegisTheme.BronzeBright : AegisTheme.TravertineDim);

        if (DockMath.IsHorizontal(edge))
        {
            drawList.AddText(font, fontSize, min + (size - textSize) / 2f, color, label);
            return;
        }

        // turned, the label is textSize.Y wide and textSize.X tall -- centred in the tab
        var topLeft = min + new Vector2((size.X - textSize.Y) / 2f, (size.Y - textSize.X) / 2f);
        var pivot = DockMath.PivotFor(topLeft, textSize);

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
    private static bool DrawPanel(Tab tab, DockEdge edge, Vector2 tabMin, Vector2 tabSize)
    {
        var viewport = ImGui.GetMainViewport();
        var (pos, pivot) = DockMath.PanelPlacement(edge, tabMin, tabSize, tab.PanelSize, viewport.WorkPos, viewport.WorkSize, PanelGap);

        ImGui.SetNextWindowPos(pos, ImGuiCond.Always, pivot);
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
/// The shared list of tabs, held in Dalamud's data share so every plugin's dock sees the same one.
/// <para>Only framework types cross the boundary — a <c>ConcurrentDictionary&lt;string, long[]&gt;</c> keyed by
/// "Owner/TabId", each value <c>[edge, along × 10000, length, order, seenMs]</c> — because the data share hands every
/// plugin the SAME object and a type defined in one plugin's assembly would not match another's. If the share cannot be
/// had, the tabs are laid out on their own.</para>
/// </summary>
public sealed class DockLayoutShare : IDisposable
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly string owner;
    private readonly int order;
    private readonly bool shared;
    private readonly ConcurrentDictionary<string, long[]> slots;

    /// <param name="owner">This plugin's name in the shared layout ("Minerva").</param>
    /// <param name="order">Settles ties with other plugins' tabs wanting the same spot, smallest first (Daedalus 10,
    /// Charon 20).</param>
    public DockLayoutShare(IDalamudPluginInterface pluginInterface, string owner, int order)
    {
        this.pluginInterface = pluginInterface;
        this.owner = owner;
        this.order = order;
        ConcurrentDictionary<string, long[]>? found = null;
        try
        {
            found = pluginInterface.GetOrCreateData(DockLayout.ShareTag, () => new ConcurrentDictionary<string, long[]>());
        }
        catch
        {
            // laid out alone, below
        }

        this.shared = found != null;
        this.slots = found ?? new ConcurrentDictionary<string, long[]>();
    }

    /// <summary>Publish this frame's tabs and get back where each one starts along its edge, keyed by tab id: its top on
    /// the left and right edges, its left on the top and bottom ones.</summary>
    public Dictionary<string, float> Place(IReadOnlyList<DockRequest> tabs, Vector2 workPos, Vector2 workSize)
    {
        var now = Environment.TickCount64;
        var prefix = this.owner + "/";
        var mine = new HashSet<string>();
        for (var i = 0; i < tabs.Count; i++)
        {
            var t = tabs[i];
            var key = prefix + t.Id;
            mine.Add(key);
            this.slots[key] = [(int)t.Edge, (long)MathF.Round(Math.Clamp(t.Along, 0f, 1f) * 10000f), (long)MathF.Ceiling(t.Length), this.order * 100 + i, now];
        }

        // a tab of ours that is gone this frame leaves at once, rather than holding its place for a second
        foreach (var key in this.slots.Keys)
            if (key.StartsWith(prefix, StringComparison.Ordinal) && !mine.Contains(key))
                this.slots.TryRemove(key, out _);

        var all = Read(this.slots).ToList();
        var result = new Dictionary<string, float>();
        foreach (var edge in tabs.Select(t => t.Edge).Distinct())
        {
            var horizontal = DockMath.IsHorizontal(edge);
            var starts = DockLayout.Resolve((int)edge, all, now, horizontal ? workPos.X : workPos.Y, horizontal ? workSize.X : workSize.Y);
            foreach (var t in tabs.Where(t => t.Edge == edge))
                if (starts.TryGetValue(prefix + t.Id, out var start))
                    result[t.Id] = start;
        }

        return result;
    }

    private static IEnumerable<DockSlot> Read(ConcurrentDictionary<string, long[]> slots)
    {
        foreach (var (key, v) in slots)
            if (v is { Length: >= 5 })
                yield return new DockSlot(key, (int)v[0], v[1] / 10000f, v[2], (int)v[3], v[4]);
    }

    public void Dispose()
    {
        var prefix = this.owner + "/";
        foreach (var key in this.slots.Keys)
            if (key.StartsWith(prefix, StringComparison.Ordinal))
                this.slots.TryRemove(key, out _);

        if (this.shared)
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
