using Dalamud.Bindings.ImGui;
using System.Reflection;

namespace Minerva.Dawntrail.Savage.M06SSugarRiot;

[ConfigDisplay(Order = 0x110, Parent = typeof(DawntrailConfig))]
public sealed class M06SSugarRiotConfig() : ConfigNode
{
    [PropertyDisplay("Enable custom priority list", tooltip: "If disabled the add phase priority list below will not be used.")]
    public bool EnablePriorityList = true;

    [PropertyDisplay("Add phase priorities for autorotation (from highest to lowest)")]
    [PropertyStringOrder(["Sugar Riot", "Mu P1", "Yan P1", "Gimme Cat P1", "Mu P2", "Feather Ray NW", "Feather Ray NE", "Yan P3", "Gimme Cat P3", "Jabberwock P3",
    "Feather Ray SW", "Feather Ray SE", "Mu P4", "Gimme Cat P4", "Jabberwock P4", "Yan P4"])]
    public int[] AddsPriorityOrder = [15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0];

}
