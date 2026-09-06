// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M02SHoneyBLovely;

sealed class StingingSlash(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(50f, 45f.Degrees()), (uint)IconID.StingingSlash, (uint)AID.StingingSlashAOE);
sealed class KillerSting(ModuleBase module) : Components.IconSharedTankbuster(module, (uint)IconID.KillerSting, (uint)AID.KillerStingAOE, 6f);

sealed class BlindingLoveBait : Components.SimpleAOEs
{
    public BlindingLoveBait(ModuleBase module) : base(module, (uint)AID.BlindingLoveBaitAOE, new AOEShapeRect(50f, 4f)) { MaxDangerColor = 2; }
}

abstract class BlindingLoveCharge(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(45f, 5f));
sealed class BlindingLoveCharge1(ModuleBase module) : BlindingLoveCharge(module, (uint)AID.BlindingLoveCharge1AOE);
sealed class BlindingLoveCharge2(ModuleBase module) : BlindingLoveCharge(module, (uint)AID.BlindingLoveCharge2AOE);

sealed class PoisonStingBait(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.PoisonStingAOE, 6f);
sealed class PoisonStingVoidzone(ModuleBase module) : Components.Voidzone(module, 6f, m => m.Enemies((uint)OID.PoisonStingVoidzone).Where(z => z.EventState != 7));
sealed class BeeSting(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.BeeStingAOE, 6f, 4, 4);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 988u, CFCID = 988u, NameID = 12685u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class M02SHoneyBLovely(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsCircle(20f));