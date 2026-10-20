using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Minerva.Ui;

/// <summary>
/// The screen edge a tab is pinned to. The values are the shared layout's edge numbers, the same in Daedalus and Charon.
/// </summary>
public enum DockEdge
{
    Left = 0,
    Right = 1,
    Top = 2,
    Bottom = 3,
}

/// <summary>
/// Which edge tab's panel is out. Pointing at a tab opens it; it stays while the pointer is on the tab or the
/// panel, and closes a moment after it leaves — the delay is what stops it flickering shut on the way across the
/// gap. Clicking a tab PINS its panel open (click again to unpin).
/// <para>Ported from Daedalus's edge dock (<c>Daedalus/Windows/Dock/EdgeDock.cs</c>, itself from Charon, the same
/// author). Pure: no Dalamud or ImGui, so it is tested headless.</para>
/// </summary>
public sealed class DockHover
{
    private DateTime lastTouchedUtc;

    public string? Open { get; private set; }
    public string? Pinned { get; private set; }

    /// <summary>The panel held out by the plugin rather than by a click — the radar while a fight is on — until
    /// <see cref="Release"/>. Like a pin, but the user's own pin outlasts it.</summary>
    public string? Held { get; private set; }

    /// <param name="hoveredTab">The tab under the pointer this frame, if any.</param>
    /// <param name="panelBusy">The pointer is on the open panel, or the panel is mid-interaction (a combo open, a slider held).</param>
    public void Update(string? hoveredTab, bool panelBusy, DateTime nowUtc, TimeSpan closeDelay, IReadOnlyList<string> visible)
    {
        // a tab that is no longer shown takes its panel (and its pin, and its hold) with it
        if (this.Pinned is { } pinned && !visible.Contains(pinned))
            this.Pinned = null;
        if (this.Held is { } held && !visible.Contains(held))
            this.Held = null;
        if (this.Open is { } open && !visible.Contains(open))
            this.Open = null;

        if (hoveredTab is { } hovered && visible.Contains(hovered))
        {
            this.Open = hovered;
            this.lastTouchedUtc = nowUtc;
            return;
        }

        if (panelBusy)
        {
            this.lastTouchedUtc = nowUtc;
            return;
        }

        if (this.Open != null && nowUtc - this.lastTouchedUtc > closeDelay)
            this.Open = this.Pinned ?? this.Held; // back to the pinned or held panel, or to nothing
    }

    /// <summary>Hold a panel out until <see cref="Release"/>: open now, and what the panel returns to when the pointer
    /// leaves another tab.</summary>
    public void Hold(string tab)
    {
        this.Held = tab;
        this.Open = tab;
    }

    /// <summary>Let a held panel go. It closes the ordinary way, after the delay and not under the pointer; a panel the
    /// user pinned stays.</summary>
    public void Release(string tab)
    {
        if (this.Held == tab)
            this.Held = null;
    }

    /// <summary>A click on a tab: pin it, or unpin it if it already was.</summary>
    public void Click(string tab)
    {
        this.Pinned = this.Pinned == tab ? null : tab;
        if (this.Pinned != null)
            this.Open = this.Pinned;
    }
}

/// <summary>
/// The dock's geometry. Sideways text first: ImGui cannot draw rotated text, so the label is drawn normally and its
/// vertices are then turned a quarter-turn anticlockwise about a pivot. Text of size (w, h) drawn at the pivot lands,
/// once turned, in a box of size (h, w) whose top-left is where the caller asked — reading bottom-to-top. Then where a
/// tab sits on its edge, where a dragged one lands, and where its panel opens. Ported from Daedalus's.
/// </summary>
public static class DockMath
{
    /// <summary>Where to draw the unrotated text so that, turned, its box has this top-left corner.</summary>
    public static Vector2 PivotFor(Vector2 topLeft, Vector2 textSize) => new(topLeft.X, topLeft.Y + textSize.X);

    /// <summary>A quarter-turn anticlockwise about <paramref name="pivot"/>, in screen space (Y grows downward).</summary>
    public static Vector2 TurnUp(Vector2 point, Vector2 pivot)
    {
        var d = point - pivot;
        return pivot + new Vector2(d.Y, -d.X);
    }

    /// <summary>
    /// A vertex of text DRAWN at <paramref name="drawnAt"/>, turned a quarter-turn and moved so the text's origin lands
    /// on <paramref name="pivot"/>. Drawing somewhere else first is not optional: ImGui drops glyphs that fall outside the
    /// screen BEFORE they are turned, so text drawn at a pivot on the right edge loses its tail (Charon's "Retainers" tab
    /// read "Reta"). Draw where the whole line is on screen, then place it with this.
    /// </summary>
    public static Vector2 TurnUpAndPlace(Vector2 point, Vector2 drawnAt, Vector2 pivot)
    {
        var d = point - drawnAt;
        return pivot + new Vector2(d.Y, -d.X);
    }

    /// <summary>Top and bottom tabs run along the edge; left and right ones stand up the edge.</summary>
    public static bool IsHorizontal(DockEdge edge) => edge is DockEdge.Top or DockEdge.Bottom;

    /// <summary>The screen edge closest to <paramref name="point"/> — where a dragged tab lands when let go.</summary>
    public static DockEdge NearestEdge(Vector2 point, Vector2 workPos, Vector2 workSize)
    {
        var best = DockEdge.Left;
        var bestDistance = point.X - workPos.X;

        void Consider(DockEdge edge, float distance)
        {
            if (distance < bestDistance)
            {
                best = edge;
                bestDistance = distance;
            }
        }

        Consider(DockEdge.Right, workPos.X + workSize.X - point.X);
        Consider(DockEdge.Top, point.Y - workPos.Y);
        Consider(DockEdge.Bottom, workPos.Y + workSize.Y - point.Y);
        return best;
    }

    /// <summary>How far along <paramref name="edge"/> a point is, as a fraction of the edge (0..1).</summary>
    public static float AlongFraction(DockEdge edge, Vector2 point, Vector2 workPos, Vector2 workSize)
    {
        var fraction = IsHorizontal(edge)
            ? (point.X - workPos.X) / MathF.Max(1f, workSize.X)
            : (point.Y - workPos.Y) / MathF.Max(1f, workSize.Y);
        return Math.Clamp(fraction, 0f, 1f);
    }

    /// <summary>
    /// A tab's rectangle: <paramref name="start"/> along the edge (its top, or its left on the top and bottom edges),
    /// <paramref name="length"/> long, <paramref name="thickness"/> out from the edge.
    /// </summary>
    public static (Vector2 Min, Vector2 Size) TabRect(
        DockEdge edge, float start, float length, float thickness, Vector2 workPos, Vector2 workSize) => edge switch
    {
        DockEdge.Right => (new Vector2(workPos.X + workSize.X - thickness, start), new Vector2(thickness, length)),
        DockEdge.Top => (new Vector2(start, workPos.Y), new Vector2(length, thickness)),
        DockEdge.Bottom => (new Vector2(start, workPos.Y + workSize.Y - thickness), new Vector2(length, thickness)),
        _ => (new Vector2(workPos.X, start), new Vector2(thickness, length)),
    };

    /// <summary>
    /// Where an open tab's panel goes: beside the tab, on the screen side of it, level with it — slid back along the
    /// edge if <paramref name="panelSize"/> would run it off the screen.
    /// </summary>
    /// <returns>The window position and the pivot that position refers to (as for SetNextWindowPos).</returns>
    public static (Vector2 Pos, Vector2 Pivot) PanelPlacement(
        DockEdge edge, Vector2 tabMin, Vector2 tabSize, Vector2 panelSize, Vector2 workPos, Vector2 workSize, float gap)
    {
        const float margin = 4f;
        var y = MathF.Max(workPos.Y + margin, MathF.Min(tabMin.Y, workPos.Y + workSize.Y - panelSize.Y - margin));
        var x = MathF.Max(workPos.X + margin, MathF.Min(tabMin.X, workPos.X + workSize.X - panelSize.X - margin));

        return edge switch
        {
            DockEdge.Right => (new Vector2(tabMin.X - gap, y), new Vector2(1f, 0f)),
            DockEdge.Top => (new Vector2(x, tabMin.Y + tabSize.Y + gap), Vector2.Zero),
            DockEdge.Bottom => (new Vector2(x, tabMin.Y - gap), new Vector2(0f, 1f)),
            _ => (new Vector2(tabMin.X + tabSize.X + gap, y), Vector2.Zero),
        };
    }
}

/// <summary>One tab as the shared layout sees it.</summary>
/// <param name="Key">"Owner/TabId" — the owner is the plugin ("Minerva", "Daedalus", "Charon").</param>
/// <param name="Edge">0 = left edge, 1 = right edge, 2 = top, 3 = bottom (<see cref="DockEdge"/>).</param>
/// <param name="Along">Where the tab wants its centre, as a fraction of the edge (0 = top / left end, 1 = the far end).</param>
/// <param name="Length">How long the tab is along the edge this frame, in pixels.</param>
/// <param name="Order">Settles ties between tabs wanting the same spot, smallest first: the plugin's order × 100 + the tab's index.</param>
/// <param name="SeenMs">When it last published, on <see cref="Environment.TickCount64"/>.</param>
public readonly record struct DockSlot(string Key, int Edge, float Along, float Length, int Order, long SeenMs)
{
    public string Owner => this.Key.IndexOf('/') is var i and >= 0 ? this.Key[..i] : this.Key;
}

/// <summary>One of this plugin's tabs, asking for a place.</summary>
public readonly record struct DockRequest(string Id, DockEdge Edge, float Along, float Length);

/// <summary>
/// Where every edge tab goes, across every plugin that docks tabs. Pure.
/// <para>Each tab sits where it was put (<see cref="DockSlot.Along"/>; a tab never moved wants the middle). Tabs that
/// would overlap form a group laid end to end, centred on the average of where its tabs want to be — so several tabs
/// all wanting the middle stack centred, and one tab dragged near another nudges both apart rather than covering it.
/// Groups are kept on screen. A tab that stops publishing drops out after <see cref="StaleMs"/> and the rest close
/// up.</para>
/// <para>The same rules live in Daedalus and Charon (<c>DockLayout.cs</c> in each). All three must agree on them, on
/// <see cref="ShareTag"/> and on the slot layout, or each lays its tabs out on its own and they draw over each other.</para>
/// </summary>
public static class DockLayout
{
    /// <summary>The data-share tag Daedalus and Charon use for the shared list; change all three or none.</summary>
    public const string ShareTag = "EdgeDock.Layout.v2";

    /// <summary>A tab that has not published for this long is gone.</summary>
    public const long StaleMs = 1000;

    /// <summary>Space between two tabs of one plugin.</summary>
    public const float TabGap = 4f;

    /// <summary>Space between two plugins' tabs — wider, so you can see where one plugin's run ends.</summary>
    public const float GroupGap = 10f;

    public static float GapBetween(DockSlot a, DockSlot b) => a.Owner == b.Owner ? TabGap : GroupGap;

    /// <summary>Where each live tab on <paramref name="edge"/> starts along it, keyed by <see cref="DockSlot.Key"/>.</summary>
    public static Dictionary<string, float> Resolve(int edge, IEnumerable<DockSlot> slots, long nowMs, float workStart, float workLength)
    {
        var live = slots
            .Where(s => s.Edge == edge && s.Length > 0f && nowMs - s.SeenMs <= StaleMs)
            .OrderBy(s => s.Along)
            .ThenBy(s => s.Order)
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .ToList();

        var groups = new List<List<DockSlot>>();
        var starts = new List<float>();
        foreach (var slot in live)
        {
            groups.Add([slot]);
            starts.Add(StartOf(groups[^1], workStart, workLength));

            // overlapping the group before it (or too close): join it, and settle the joined group afresh
            while (groups.Count >= 2)
            {
                var a = groups.Count - 2;
                var b = a + 1;
                if (starts[a] + LengthOf(groups[a]) + GapBetween(groups[a][^1], groups[b][0]) <= starts[b])
                    break;

                groups[a].AddRange(groups[b]);
                groups.RemoveAt(b);
                starts.RemoveAt(b);
                starts[a] = StartOf(groups[a], workStart, workLength);
            }
        }

        var result = new Dictionary<string, float>();
        for (var g = 0; g < groups.Count; g++)
        {
            var at = starts[g];
            var group = groups[g];
            for (var i = 0; i < group.Count; i++)
            {
                result[group[i].Key] = at;
                if (i + 1 < group.Count)
                    at += group[i].Length + GapBetween(group[i], group[i + 1]);
            }
        }

        return result;
    }

    private static float LengthOf(List<DockSlot> group)
    {
        var length = 0f;
        for (var i = 0; i < group.Count; i++)
            length += group[i].Length + (i + 1 < group.Count ? GapBetween(group[i], group[i + 1]) : 0f);
        return length;
    }

    /// <summary>Each tab wants its centre at its <see cref="DockSlot.Along"/>; the group is centred on the average of
    /// those, kept on screen. Tabs that all want one spot are therefore centred on it as one run.</summary>
    private static float StartOf(List<DockSlot> group, float workStart, float workLength)
    {
        var wanted = 0f;
        foreach (var slot in group)
            wanted += workStart + Math.Clamp(slot.Along, 0f, 1f) * workLength;
        wanted /= group.Count;

        var length = LengthOf(group);
        var latest = MathF.Max(workStart, workStart + workLength - length);
        return Math.Clamp(wanted - length / 2f, workStart, latest);
    }
}
