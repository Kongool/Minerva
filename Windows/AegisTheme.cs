using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Minerva.Windows;

/// <summary>
/// The Aegis palette: basalt ground, travertine text, bronze accent, Tyrian purple for what is selected.
/// <para>The colours are chosen around a constraint rather than a mood. The radar's own colours are a
/// language — red means this will hurt you, green means stand here — so the chrome may not use red or
/// green for anything, or the eye is pulled to the furniture instead of the mechanic. That rules out the
/// two obvious Roman choices (legion vermilion, bronze verdigris) and leaves the two that sit furthest
/// from the signals in hue: bronze, and the purple Rome reserved for people who mattered. Here it is
/// reserved for the one row that is currently selected.</para>
/// <para>Neutrals are warm rather than grey — a true grey beside bronze reads as unconsidered.</para>
/// </summary>
public static class AegisTheme
{
    public static readonly Vector4 Basalt = Hex(0x16130F);
    public static readonly Vector4 BasaltRaised = Hex(0x201B16);
    public static readonly Vector4 BasaltSunken = Hex(0x100D0A);
    public static readonly Vector4 Rule = Hex(0x362E25);
    public static readonly Vector4 Travertine = Hex(0xE8DFD1);
    public static readonly Vector4 TravertineDim = Hex(0xA2968A);
    public static readonly Vector4 Bronze = Hex(0xB08D4F);
    public static readonly Vector4 BronzeBright = Hex(0xC9A461);
    public static readonly Vector4 Tyrian = Hex(0x6E2A5B);
    public static readonly Vector4 TyrianBright = Hex(0x9A5A87);

    private static Vector4 Hex(uint rgb, float a = 1f)
        => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

    private static Vector4 Alpha(Vector4 c, float a) => new(c.X, c.Y, c.Z, a);

    /// <summary>
    /// Apply the palette for one window. Returns the counts to hand back to <see cref="Pop"/> — ImGui's
    /// colour and variable stacks are global, so every push has to be balanced or the next window inherits
    /// a palette it never asked for.
    /// </summary>
    public static (int Colors, int Vars) Push()
    {
        var c = 0;
        void Col(ImGuiCol which, Vector4 value)
        {
            ImGui.PushStyleColor(which, value);
            ++c;
        }

        Col(ImGuiCol.WindowBg, Basalt);
        Col(ImGuiCol.ChildBg, Alpha(BasaltSunken, 0.45f));
        Col(ImGuiCol.PopupBg, BasaltRaised);
        Col(ImGuiCol.Border, Rule);
        Col(ImGuiCol.Text, Travertine);
        Col(ImGuiCol.TextDisabled, TravertineDim);

        Col(ImGuiCol.TitleBg, BasaltSunken);
        Col(ImGuiCol.TitleBgActive, Alpha(Tyrian, 0.55f));
        Col(ImGuiCol.TitleBgCollapsed, Alpha(BasaltSunken, 0.75f));

        Col(ImGuiCol.FrameBg, BasaltSunken);
        Col(ImGuiCol.FrameBgHovered, Alpha(Bronze, 0.22f));
        Col(ImGuiCol.FrameBgActive, Alpha(Bronze, 0.34f));

        Col(ImGuiCol.Button, BasaltRaised);
        Col(ImGuiCol.ButtonHovered, Alpha(Bronze, 0.30f));
        Col(ImGuiCol.ButtonActive, Alpha(Bronze, 0.46f));

        // headers are selection: this is the one place Tyrian is allowed
        Col(ImGuiCol.Header, Alpha(Tyrian, 0.42f));
        Col(ImGuiCol.HeaderHovered, Alpha(Tyrian, 0.58f));
        Col(ImGuiCol.HeaderActive, Alpha(Tyrian, 0.72f));

        Col(ImGuiCol.Separator, Rule);
        Col(ImGuiCol.SeparatorHovered, Bronze);
        Col(ImGuiCol.SeparatorActive, BronzeBright);

        Col(ImGuiCol.CheckMark, BronzeBright);
        Col(ImGuiCol.SliderGrab, Bronze);
        Col(ImGuiCol.SliderGrabActive, BronzeBright);

        Col(ImGuiCol.Tab, BasaltSunken);
        Col(ImGuiCol.TabHovered, Alpha(Bronze, 0.34f));
        Col(ImGuiCol.TabActive, Alpha(Tyrian, 0.50f));

        Col(ImGuiCol.ScrollbarBg, BasaltSunken);
        Col(ImGuiCol.ScrollbarGrab, Rule);
        Col(ImGuiCol.ScrollbarGrabHovered, Alpha(Bronze, 0.5f));
        Col(ImGuiCol.ScrollbarGrabActive, Bronze);

        var v = 0;
        void Var(ImGuiStyleVar which, float value)
        {
            ImGui.PushStyleVar(which, value);
            ++v;
        }

        // carved rather than moulded: a small radius and a real border suit stone better than soft cards
        Var(ImGuiStyleVar.FrameRounding, 2f);
        Var(ImGuiStyleVar.GrabRounding, 2f);
        Var(ImGuiStyleVar.WindowBorderSize, 1f);
        Var(ImGuiStyleVar.FrameBorderSize, 1f);

        return (c, v);
    }

    public static void Pop((int Colors, int Vars) pushed)
    {
        if (pushed.Vars > 0)
            ImGui.PopStyleVar(pushed.Vars);
        if (pushed.Colors > 0)
            ImGui.PopStyleColor(pushed.Colors);
    }
}
