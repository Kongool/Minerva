using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Minerva.Windows;

/// <summary>
/// Per-encounter strategy settings, rendered from the attributes on each <see cref="ConfigNode"/>.
///
/// <para><b>Why this is generated rather than hand-drawn.</b> There are 37 config nodes and they grow with
/// every ported fight. A node declares what it needs — a label, a range, the names of its groups — and this
/// window works out the control. Adding settings to a new module means writing the fields, nothing here.</para>
///
/// <para><b>Why these settings matter.</b> Most are not cosmetic. A raid module resolves mechanics according
/// to a plan — which group goes north, which tank baits, whether you run the uptime variant — and a module
/// resolving the opposite way from your party is worse than no module at all. This window is where that plan
/// is stated.</para>
/// </summary>
public sealed class EncounterSettingsWindow : Window
{
    private readonly ConfigRoot config;
    private string filter = string.Empty;

    public EncounterSettingsWindow(ConfigRoot config)
        : base("Encounter settings###MinervaEncounterSettings")
    {
        this.config = config;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    private (int Colors, int Vars) theme;

    public override void PreDraw() => this.theme = AegisTheme.Push();

    public override void PostDraw() => AegisTheme.Pop(this.theme);

    public override void Draw()
    {
        ImGui.TextDisabled("Strategy settings for individual fights. Defaults match BossmodReborn's.");
        ImGui.Separator();

        var f = this.filter;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputTextWithHint("##filter", "filter by fight or setting...", ref f, 128))
            this.filter = f;
        ImGui.Spacing();

        var nodes = this.config.Nodes
            .Select(n => (Node: n, Display: n.GetType().GetCustomAttribute<ConfigDisplayAttribute>()))
            .Where(x => this.Matches(x.Node, x.Display))
            .OrderBy(x => x.Display?.Order ?? int.MaxValue)
            .ThenBy(x => NameOf(x.Node.GetType(), x.Display), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (nodes.Count == 0)
        {
            ImGui.TextDisabled("Nothing matches that filter.");
            return;
        }

        foreach (var (node, display) in nodes)
        {
            var fields = SettingFields(node).ToList();
            if (fields.Count == 0)
                continue; // a grouping node that only exists to parent others

            if (!ImGui.CollapsingHeader($"{NameOf(node.GetType(), display)}###{node.GetType().FullName}"))
                continue;

            ImGui.Indent();
            var changed = false;
            foreach (var (field, prop) in fields)
            {
                if (prop.Separator)
                    ImGui.Separator();
                changed |= this.DrawField(node, field, prop);
            }
            ImGui.Unindent();

            if (changed)
                node.NotifyModified();
        }
    }

    private bool Matches(ConfigNode node, ConfigDisplayAttribute? display)
    {
        if (this.filter.Length == 0)
            return true;
        if (NameOf(node.GetType(), display).Contains(this.filter, StringComparison.OrdinalIgnoreCase))
            return true;
        foreach (var (_, prop) in SettingFields(node))
            if (prop.Label.Contains(this.filter, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // A public field is only a setting if it carries a label; anything else is state the module keeps.
    private static IEnumerable<(FieldInfo Field, PropertyDisplayAttribute Prop)> SettingFields(ConfigNode node)
    {
        foreach (var f in node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (f.GetCustomAttribute<PropertyDisplayAttribute>() is { } p)
                yield return (f, p);
    }

    private static string NameOf(Type t, ConfigDisplayAttribute? display)
    {
        if (!string.IsNullOrEmpty(display?.Name))
            return display!.Name!;
        var n = t.Name;
        return n.EndsWith("Config", StringComparison.Ordinal) ? n[..^"Config".Length] : n;
    }

    private bool DrawField(ConfigNode node, FieldInfo field, PropertyDisplayAttribute prop)
    {
        var id = $"##{node.GetType().Name}.{field.Name}";
        var value = field.GetValue(node);
        var changed = false;

        switch (value)
        {
            case bool b when field.GetCustomAttribute<PropertyComboAttribute>() is { } combo && combo.Values.Length == 2:
                {
                    var idx = b ? 1 : 0;
                    ImGui.TextWrapped(prop.Label);
                    ImGui.SetNextItemWidth(-1f);
                    if (ImGui.Combo(id, ref idx, combo.Values, combo.Values.Length))
                    {
                        field.SetValue(node, idx != 0);
                        changed = true;
                    }
                    break;
                }
            case bool b:
                {
                    var v = b;
                    if (ImGui.Checkbox($"{prop.Label}{id}", ref v))
                    {
                        field.SetValue(node, v);
                        changed = true;
                    }
                    break;
                }
            case float fl:
                {
                    var v = fl;
                    ImGui.TextWrapped(prop.Label);
                    ImGui.SetNextItemWidth(-1f);
                    var slider = field.GetCustomAttribute<PropertySliderAttribute>();
                    var ok = slider != null
                        ? ImGui.SliderFloat(id, ref v, slider.Min, slider.Max)
                        : ImGui.InputFloat(id, ref v);
                    if (ok)
                    {
                        field.SetValue(node, v);
                        changed = true;
                    }
                    break;
                }
            case int i when field.GetCustomAttribute<PropertyComboAttribute>() is { } combo:
                {
                    var v = i;
                    ImGui.TextWrapped(prop.Label);
                    ImGui.SetNextItemWidth(-1f);
                    if (ImGui.Combo(id, ref v, combo.Values, combo.Values.Length))
                    {
                        field.SetValue(node, v);
                        changed = true;
                    }
                    break;
                }
            case int i:
                {
                    var v = i;
                    ImGui.TextWrapped(prop.Label);
                    ImGui.SetNextItemWidth(-1f);
                    var slider = field.GetCustomAttribute<PropertySliderAttribute>();
                    var ok = slider != null
                        ? ImGui.SliderInt(id, ref v, (int)slider.Min, (int)slider.Max)
                        : ImGui.InputInt(id, ref v);
                    if (ok)
                    {
                        field.SetValue(node, v);
                        changed = true;
                    }
                    break;
                }
            case Color c:
                {
                    var v = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
                    ImGui.TextWrapped(prop.Label);
                    if (ImGui.ColorEdit4(id, ref v, ImGuiColorEditFlags.AlphaBar))
                    {
                        field.SetValue(node, Color.FromComponents((uint)(v.X * 255f), (uint)(v.Y * 255f), (uint)(v.Z * 255f), (uint)(v.W * 255f)));
                        changed = true;
                    }
                    break;
                }
            case GroupAssignment ga:
                changed = DrawGroupAssignment(id, prop, field, ga);
                break;
            case Enum e:
                {
                    var names = Enum.GetNames(field.FieldType);
                    var idx = Array.IndexOf(names, e.ToString());
                    ImGui.TextWrapped(prop.Label);
                    ImGui.SetNextItemWidth(-1f);
                    if (ImGui.Combo(id, ref idx, names, names.Length) && idx >= 0)
                    {
                        field.SetValue(node, Enum.Parse(field.FieldType, names[idx]));
                        changed = true;
                    }
                    break;
                }
            case int[] order when field.GetCustomAttribute<PropertyStringOrderAttribute>() is { } so:
                changed = DrawOrder(id, prop, so, order);
                break;
            default:
                ImGui.TextWrapped(prop.Label);
                ImGui.SameLine();
                ImGui.TextDisabled($"({field.FieldType.Name} — not editable here)");
                break;
        }

        if (prop.Tooltip.Length > 0 && ImGui.IsItemHovered())
            ImGui.SetTooltip(prop.Tooltip);
        return changed;
    }

    /// <summary>
    /// One row per role, each picking the group that role takes. Presets from
    /// <see cref="GroupPresetAttribute"/> set the whole thing at once, which is how these are actually used —
    /// a party agrees on "TMRH" and picks it, rather than setting eight dropdowns.
    /// </summary>
    private static bool DrawGroupAssignment(string id, PropertyDisplayAttribute prop, FieldInfo field, GroupAssignment ga)
    {
        ImGui.TextWrapped(prop.Label);

        var groupNames = field.GetCustomAttribute<GroupDetailsAttribute>()?.Names
            ?? Enumerable.Range(0, 8).Select(i => i.ToString()).ToArray();
        var changed = false;

        var presets = field.GetCustomAttributes<GroupPresetAttribute>().ToList();
        if (presets.Count > 0)
        {
            foreach (var p in presets)
            {
                if (ImGui.SmallButton($"{p.Name}{id}.{p.Name}"))
                {
                    Array.Copy(p.Preset, ga.Assignments, Math.Min(p.Preset.Length, ga.Assignments.Length));
                    changed = true;
                }
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }

        var roles = Enum.GetNames<PartyRolesConfig.Assignment>();
        if (ImGui.BeginTable($"{id}.table", 2, ImGuiTableFlags.SizingStretchProp))
        {
            for (var role = 0; role < ga.Assignments.Length && role < roles.Length; ++role)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(roles[role]);
                ImGui.TableNextColumn();
                var idx = ga.Assignments[role];
                ImGui.SetNextItemWidth(-1f);
                if (ImGui.Combo($"{id}.{role}", ref idx, groupNames, groupNames.Length))
                {
                    ga.Assignments[role] = idx;
                    changed = true;
                }
            }
            ImGui.EndTable();
        }

        if (!ga.Validate())
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.2f, 1f), "! incomplete — the module will not use this until every group is filled exactly once");

        return changed;
    }

    private static bool DrawOrder(string id, PropertyDisplayAttribute prop, PropertyStringOrderAttribute so, int[] order)
    {
        ImGui.TextWrapped(prop.Label);
        var changed = false;
        for (var i = 0; i < order.Length; ++i)
        {
            var name = order[i] >= 0 && order[i] < so.Values.Length ? so.Values[order[i]] : order[i].ToString();
            ImGui.TextUnformatted($"{i + 1}. {name}");
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton($"^{id}.{i}"))
                {
                    (order[i - 1], order[i]) = (order[i], order[i - 1]);
                    changed = true;
                }
            }
        }
        return changed;
    }
}
