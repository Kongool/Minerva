// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M06NSugarRiot;

sealed class SprayPain : Components.SimpleAOEs
{
    public SprayPain(ModuleBase module) : base(module, (uint)AID.SprayPain, 10f, 10)
    {
        MaxDangerColor = 5;
    }
}

sealed class LightningBolt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightningBolt, 4f);

abstract class ColorRiot(ModuleBase module, uint aid, bool showhint) : Components.BaitAwayCast(module, aid, 4f, tankbuster: showhint);
sealed class WarmBomb(ModuleBase module) : ColorRiot(module, (uint)AID.WarmBomb, true);
sealed class CoolBomb(ModuleBase module) : ColorRiot(module, (uint)AID.CoolBomb, false);

sealed class MousseTouchUp(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.MousseTouchUp, 6f);
sealed class TasteOfThunder(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.TasteOfThunder, 6f);
sealed class TasteOfFire(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.TasteOfFire, 6f, 4, 4);

sealed class MousseMural(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.MousseMural);

[ModuleInfo(CFCID = 1021u, NameID = 13822u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M06NSugarRiot(WorldState ws, Actor primary) : SugarRiotSharedBounds(ws, primary);
