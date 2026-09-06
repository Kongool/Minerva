using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel.Sheets;

namespace Minerva.Global.DeepDungeon;

[ConfigDisplay(Name = "Auto-DeepDungeon (Experimental)", Parent = typeof(ModuleConfig))]
public sealed class AutoDDConfig : ConfigNode
{
    public enum ClearBehavior
    {
        [PropertyDisplay("Do not auto target")]
        None,
        [PropertyDisplay("Stop when passage opens")]
        Passage,
        [PropertyDisplay("Target everything if not at level cap, otherwise stop when passage opens")]
        Leveling,
        [PropertyDisplay("Target everything")]
        All,
    }

    [PropertyDisplay("Enable module", tooltip: "WARNING: This feature is very experimental and most likely will contain bugs or unintended behavior.\nTo enable this feature in its current state, you must activate 'Work-in-Progress' maturity modules in the `Full Duty Automation` tab.")]
    public bool Enable = true;

    [PropertyDisplay("Enable minimap")]
    public bool EnableMinimap = true;

    [PropertyDisplay("Minimap scale")]
    [PropertySlider(min: 0.2f, max: 3, Speed = 0.05f)]
    public float MinimapScale = 1.0f;

    [PropertyDisplay("Try to avoid traps", tooltip: "Avoid known trap locations sourced from PalacePal data. Does not need PalacePal installed since data is included in BMR. (Traps revealed by a Pomander of Sight will always be avoided regardless of this setting.)")]
    public bool TrapHints = true;
    [PropertyDisplay("Automatically navigate to Cairn of Passage")]
    public bool AutoPassage = true;

    [PropertyDisplay("Automatic mob targeting behavior")]
    public ClearBehavior AutoClear = ClearBehavior.Leveling;

    [PropertyDisplay("Disable DoTs on non-boss floors (only affects BMR autorotation)")]
    public bool ForbidDOTs = false;

    [PropertyDisplay("Max number of mobs to pull before pausing navigation (0 = do not navigate while in combat)")]
    [PropertySlider(0, 15)]
    public int MaxPull = 0;
    [PropertyDisplay("Try to use terrain to LOS attacks")]
    public bool AutoLOS = false;

    [PropertyDisplay("Automatically navigate to coffers")]
    public bool AutoMoveTreasure = true;
    [PropertyDisplay("Prioritize opening coffers over Cairn of Passage")]
    public bool OpenChestsFirst = false;
    [PropertyDisplay("Open gold coffers")]
    public bool GoldCoffer = true;
    [PropertyDisplay("Open silver coffers")]
    public bool SilverCoffer = true;
    [PropertyDisplay("Open bronze coffers")]
    public bool BronzeCoffer = true;

    [PropertyDisplay("Reveal all rooms before proceeding to next floor")]
    public bool FullClear = false;

    public BitMask AutoPoms;
    public BitMask AutoMagicite;
    public BitMask AutoDemiclone;

}
