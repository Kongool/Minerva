using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Minerva.Ui;

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

    /// <param name="hoveredTab">The tab under the pointer this frame, if any.</param>
    /// <param name="panelBusy">The pointer is on the open panel, or the panel is mid-interaction (a combo open, a slider held).</param>
    public void Update(string? hoveredTab, bool panelBusy, DateTime nowUtc, TimeSpan closeDelay, IReadOnlyList<string> visible)
    {
        // a tab that is no longer shown takes its panel (and its pin) with it
        if (this.Pinned is { } pinned && !visible.Contains(pinned))
            this.Pinned = null;
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
            this.Open = this.Pinned; // back to the pinned panel, or to nothing
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
/// Sideways text. ImGui cannot draw rotated text, so the label is drawn normally and its vertices are then turned a
/// quarter-turn anticlockwise about a pivot. Text of size (w, h) drawn at the pivot lands, once turned, in a box of
/// size (h, w) whose top-left is where the caller asked — reading bottom-to-top.
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
}

/// <summary>One plugin's dock as the shared layout sees it.</summary>
/// <param name="Owner">The plugin that owns the dock ("Minerva", "Daedalus", "Charon").</param>
/// <param name="Edge">0 = left edge, 1 = right edge.</param>
/// <param name="Height">How tall its stack of tabs is this frame, in pixels.</param>
/// <param name="Order">Where it sits in the group, smallest first (top). Ties fall back to the owner's name.</param>
/// <param name="SeenMs">When it last published, on <see cref="Environment.TickCount64"/>.</param>
public readonly record struct DockSlot(string Owner, int Edge, float Height, int Order, long SeenMs);

/// <summary>
/// Where each plugin's edge dock goes when several share an edge. Pure.
/// <para>The live docks on an edge are laid out as ONE group — stacked by <see cref="DockSlot.Order"/>, the group
/// centred — and each dock takes its own place in it. A dock that stops publishing (closed, no tab applies, plugin
/// unloaded) drops out after <see cref="StaleMs"/> and the rest close up.</para>
/// <para>The same rules live in Daedalus and Charon. All three must agree on them, on the share tag and on the slot
/// layout, or each falls back to centring alone and they draw on top of each other.</para>
/// </summary>
public static class DockLayout
{
    /// <summary>A dock that has not published for this long is gone.</summary>
    public const long StaleMs = 1000;

    /// <summary>Space between two plugins' docks — wider than the gap between one dock's own tabs.</summary>
    public const float GroupGap = 10f;

    /// <summary>The data-share tag Daedalus and Charon use for the shared list; change all three or none.</summary>
    public const string ShareTag = "EdgeDock.Layout.v1";

    /// <summary>
    /// The top of <paramref name="owner"/>'s dock. <paramref name="slots"/> must already include the owner's own slot
    /// for this frame; if it does not, the dock is centred on its own.
    /// </summary>
    public static float TopFor(string owner, int edge, float ownHeight, IEnumerable<DockSlot> slots, long nowMs, float workTop, float workHeight)
    {
        var live = slots
            .Where(s => s.Edge == edge && s.Height > 0f && nowMs - s.SeenMs <= StaleMs)
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Owner, StringComparer.Ordinal)
            .ToList();

        var total = live.Sum(s => s.Height) + GroupGap * Math.Max(0, live.Count - 1);
        var top = workTop + MathF.Max(0f, (workHeight - total) / 2f);
        foreach (var slot in live)
        {
            if (slot.Owner == owner)
                return top;
            top += slot.Height + GroupGap;
        }

        return workTop + MathF.Max(0f, (workHeight - ownHeight) / 2f);
    }
}
