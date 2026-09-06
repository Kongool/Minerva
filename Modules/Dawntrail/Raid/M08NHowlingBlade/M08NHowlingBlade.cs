// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M08NHowlingBlade;

sealed class ExtraplanarTitanicPursuit(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.ExtraplanarPursuit, (uint)AID.TitanicPursuit]);
sealed class RavenousSaber(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RavenousSaber5, "Raidwide x5");
sealed class GreatDivide(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.GreatDivide, new AOEShapeRect(60f, 3f));
sealed class Heavensearth1(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Heavensearth1, 6, 8, 8);
sealed class Heavensearth2(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Heavensearth2, 6, 8, 8);
sealed class Gust(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Gust, 5f);
sealed class TargetedQuake(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TargetedQuake, 4f);
sealed class GrowlingWindWealofStone(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.GrowlingWind, (uint)AID.WealOfStone1, (uint)AID.WealOfStone2, (uint)AID.WealOfStone3],
new AOEShapeRect(40f, 3f));

sealed class FangedCharge : Components.SimpleAOEs
{
    public FangedCharge(ModuleBase module) : base(module, (uint)AID.FangedCharge, new AOEShapeRect(46f, 3f))
    {
        MaxDangerColor = 2;
        MaxRisky = 2;
    }
}

sealed class MoonbeamsBite : Components.SimpleAOEGroups
{
    public MoonbeamsBite(ModuleBase module) : base(module, [(uint)AID.MoonbeamsBite1, (uint)AID.MoonbeamsBite2, (uint)AID.MoonbeamsBite3,
    (uint)AID.MoonbeamsBite4], new AOEShapeRect(40f, 10f), 2, 6)
    {
        MaxDangerColor = 1;
    }
}

sealed class RoaringWindShadowchase(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.RoaringWind, (uint)AID.Shadowchase1, (uint)AID.Shadowchase2], new AOEShapeRect(40f, 4f));
sealed class TerrestrialTitans(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TerrestrialTitans, 3f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1025u, CFCID = 1025u, NameID = 13843u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M08NHowlingBlade(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, startingArena)
{
    public static readonly WPos ArenaCenter = new(100f, 100f);
    private static readonly ArenaBoundsCustom startingArena = new([new Polygon(ArenaCenter, 17f, 40)]);
    public static readonly Polygon[] EndArenaPolygon = [new Polygon(ArenaCenter, 12f, 40)]; // 11.2s after 0x200010 then 0x00 20001
    public static readonly ArenaBoundsCustom EndArena = new(EndArenaPolygon);
}
