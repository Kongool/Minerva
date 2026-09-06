// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M12SLindwurm;

sealed class Slaughtershed0(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Slaughtershed0);
sealed class unk_46194(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.unk_46194);
sealed class RavenousReach1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RavenousReach1, new AOEShapeCone(35f, 60f.Degrees()));
sealed class PhagocyteSpotlight0(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PhagocyteSpotlight0, 5f);
sealed class GrandEntrance0(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandEntrance0, 2f);
sealed class GrandEntrance1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandEntrance1, 2f);
sealed class GrandEntrance2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandEntrance2, 2f);
sealed class GrandEntrance3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandEntrance3, 2f);
sealed class BringDownTheHouse0(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BringDownTheHouse0, new AOEShapeRect(10f, 10f));
sealed class BringDownTheHouse1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BringDownTheHouse1, new AOEShapeRect(10f, 7.5f));
sealed class BringDownTheHouse2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BringDownTheHouse2, new AOEShapeRect(10f, 5f));
sealed class DramaticLysis0(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DramaticLysis0, 6f);
sealed class DramaticLysis1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DramaticLysis1, 9f);
sealed class DramaticLysis2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DramaticLysis2, 4f);
sealed class DramaticLysis4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DramaticLysis4, 6f);
sealed class FourthWallFusion0(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FourthWallFusion0, 6f);
//sealed class FourthWallFusion1(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.FourthWallFusion1, 6f);
sealed class SplitScourge1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SplitScourge1, new AOEShapeRect(60f, 5f));
sealed class RoilingMass0(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RoilingMass0, 3f);
sealed class RoilingMass1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RoilingMass1, 3f);
sealed class PhagocyteSpotlight1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PhagocyteSpotlight1, 5f);
//sealed class VisceralBurst(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.VisceralBurst, 6f);
sealed class TheFixer(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TheFixer);
sealed class UnmitigatedExplosion(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.UnmitigatedExplosion);
sealed class WingedScourge2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WingedScourge2, new AOEShapeCone(50f, 15f.Degrees()));
sealed class TopTierSlam1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TopTierSlam1, 5f);
sealed class MightyMagic1(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.MightyMagic1, 5f);
sealed class FirefallSplash1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FirefallSplash1, 5f);
sealed class ManaBurst1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ManaBurst1, 20f);
sealed class HeavySlam0(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeavySlam0, 5f);
sealed class HeavySlam2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeavySlam2, 5f);
sealed class EsotericFinisher(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.EsotericFinisher, 10f);
//sealed class FourthWallFusion2(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.FourthWallFusion2, 6f);
sealed class unk_46395(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.unk_46395, new AOEShapeDonut(20f, 30f));
sealed class Metamitosis1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Metamitosis1, 3f);
sealed class SerpentineScourge2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SerpentineScourge2, new AOEShapeRect(30f, 10f));
sealed class RaptorKnuckles2(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.RaptorKnuckles2, 30f);
sealed class UnmitigatedImpact(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.UnmitigatedImpact);
sealed class ArcadiaAflame(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ArcadiaAflame);
sealed class RefreshingOverkill1(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RefreshingOverkill1);
sealed class RefreshingOverkill2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RefreshingOverkill2);
sealed class Splattershed2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Splattershed2);
sealed class unk_48028(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.unk_48028);

[ModuleInfo(CFCID = 0u, NameID = 0u, PrimaryActorOID = (uint)OID.Lindwurm1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "SRP, additional enhancements by Topas (ported from BMR)")]
public sealed class M12SLindwurm(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, DefaultBounds)
{
    private static readonly WPos arenaCenter = new(100f, 100f);
    public static readonly ArenaBoundsRect DefaultBounds = new(20f, 15f);
}
