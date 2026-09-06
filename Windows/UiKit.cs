using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Minerva.Windows;

/// <summary>
/// The handful of widgets the Aegis look is made of and ImGui does not ship: a segmented switch, a toggle
/// that is filled when on, a framed label, a card with a title strip. All drawn from the theme's own
/// colours so they cannot drift from it.
/// </summary>
internal static class UiKit
{
    public static readonly Vector4 Amber = new(1f, 0.7f, 0.2f, 1f);
    public static readonly Vector4 Red = new(1f, 0.3f, 0.3f, 1f);
    public static readonly Vector4 Green = new(0.3f, 1f, 0.3f, 1f);
    public static readonly Vector4 Yellow = new(1f, 0.85f, 0.2f, 1f);
    public static readonly Vector4 Grey = new(0.8f, 0.8f, 0.8f, 1f);

    private const float CardPad = 8f;

    public static Vector4 Alpha(Vector4 c, float a) => new(c.X, c.Y, c.Z, a);

    public static uint U32(Vector4 c) => ImGui.ColorConvertFloat4ToU32(c);

    /// <summary>Tooltip on the last item. Help lives here rather than beside the control: that is what lets
    /// a page of settings fit at once, and the label alone is enough once you know what it does.</summary>
    public static void Tip(string? help)
    {
        if (help != null && ImGui.IsItemHovered())
            ImGui.SetTooltip(help);
    }

    /// <summary>One item of a segmented switch. Callers join items with <c>SameLine(0, 0)</c>.</summary>
    public static bool Seg(string label, bool on)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
        ImGui.PushStyleColor(ImGuiCol.Button, on ? Alpha(AegisTheme.Tyrian, 0.55f) : AegisTheme.BasaltSunken);
        ImGui.PushStyleColor(ImGuiCol.Text, on ? AegisTheme.Travertine : AegisTheme.TravertineDim);
        var clicked = ImGui.Button(label);
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar();
        return clicked;
    }

    /// <summary>A button that reads as a state: Tyrian-filled when on, plain when off.</summary>
    public static bool Toggle(string label, bool on, Vector4? onFill = null, Vector4? onBorder = null)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, on ? onFill ?? Alpha(AegisTheme.Tyrian, 0.58f) : AegisTheme.BasaltRaised);
        ImGui.PushStyleColor(ImGuiCol.Border, on ? onBorder ?? AegisTheme.TyrianBright : AegisTheme.Rule);
        var clicked = ImGui.Button(label);
        ImGui.PopStyleColor(2);
        return clicked;
    }

    /// <summary>A framed label. Not a control — it states something.</summary>
    public static void Pill(string text, Vector4 border, Vector4 color)
    {
        var pad = new Vector2(6f, 3f);
        var size = ImGui.CalcTextSize(text) + (pad * 2f);
        var pos = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(pos, pos + size, U32(AegisTheme.BasaltSunken), 2f);
        dl.AddRect(pos, pos + size, U32(border), 2f);
        dl.AddText(pos + pad, U32(color), text);
        ImGui.Dummy(size);
    }

    public static float PillWidth(string text) => ImGui.CalcTextSize(text).X + 12f;

    private static Vector2 cardStart;
    private static float cardWidth;

    /// <summary>
    /// Open a card: a title strip, then the caller's content, closed by <see cref="EndCard"/> which draws the
    /// border around whatever was drawn. Border-after-content is what makes this safe inside a table cell —
    /// tables own the draw list's channel splitter, so a card cannot paint a background underneath.
    /// </summary>
    public static void BeginCard(string title, string? status = null)
    {
        ImGui.PushID(title);
        cardStart = ImGui.GetCursorScreenPos();
        cardWidth = ImGui.GetContentRegionAvail().X;
        var dl = ImGui.GetWindowDrawList();
        var headH = ImGui.GetTextLineHeight() + 8f;
        dl.AddRectFilled(cardStart, cardStart + new Vector2(cardWidth, headH), U32(Alpha(AegisTheme.Tyrian, 0.25f)));
        dl.AddText(cardStart + new Vector2(CardPad, 4f), U32(AegisTheme.BronzeBright), title.ToUpperInvariant());
        if (status != null)
        {
            var w = ImGui.CalcTextSize(status).X;
            dl.AddText(cardStart + new Vector2(cardWidth - w - CardPad, 4f), U32(AegisTheme.TravertineDim), status);
        }

        ImGui.SetCursorScreenPos(cardStart + new Vector2(CardPad, headH + 6f));
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + cardWidth - (CardPad * 2f));
        ImGui.BeginGroup();
    }

    /// <param name="fill">Extend the border to the bottom of the current window rather than the content, for a
    /// card that is meant to be a column.</param>
    public static void EndCard(bool fill = false)
    {
        ImGui.EndGroup();
        ImGui.PopTextWrapPos();
        var bottom = fill ? ImGui.GetWindowPos().Y + ImGui.GetWindowHeight() - 1f : ImGui.GetItemRectMax().Y + CardPad;
        ImGui.GetWindowDrawList().AddRect(cardStart, new Vector2(cardStart.X + cardWidth, bottom), U32(AegisTheme.Rule));
        ImGui.SetCursorScreenPos(new Vector2(cardStart.X, bottom));
        ImGui.Dummy(new Vector2(cardWidth, 4f));
        ImGui.PopID();
    }

    /// <summary>A small uppercase label, for naming a group inside a card.</summary>
    public static void Eyebrow(string text) => ImGui.TextColored(AegisTheme.TravertineDim, text.ToUpperInvariant());

    /// <summary>Text with a dark outline, for drawing over the arena where any colour may be underneath.</summary>
    public static void Shadowed(ImDrawListPtr dl, Vector2 pos, uint color, string text)
    {
        const uint shadow = 0xC0000000u;
        dl.AddText(pos + new Vector2(1f, 1f), shadow, text);
        dl.AddText(pos + new Vector2(-1f, 1f), shadow, text);
        dl.AddText(pos + new Vector2(1f, -1f), shadow, text);
        dl.AddText(pos + new Vector2(-1f, -1f), shadow, text);
        dl.AddText(pos, color, text);
    }
}
