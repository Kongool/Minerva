// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M01SBlackCat;

sealed class BiscuitMaker(ModuleBase module) : Components.TankSwap(module, (uint)AID.BiscuitMaker, (uint)AID.BiscuitMaker, (uint)AID.BiscuitMakerSecond, default, 2d);
sealed class QuadrupleSwipeBoss(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.QuadrupleSwipeBossAOE, 4f, 2, 2);
sealed class DoubleSwipeBoss(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DoubleSwipeBossAOE, 5f, 4, 4);
sealed class QuadrupleSwipeShade(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.QuadrupleSwipeShadeAOE, 4f, 2, 2);
sealed class DoubleSwipeShade(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DoubleSwipeShadeAOE, 5f, 4, 4);
sealed class Nailchipper(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.NailchipperAOE, 5f);
sealed class TempestuousTear(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.TempestuousTearTargetSelect, (uint)AID.TempestuousTearAOE, 5d, 100f, 3f, 1, 4);
sealed class Overshadow(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.OvershadowTargetSelect, (uint)AID.OvershadowAOE, 5.1d, 100f, 2.5f, 7, 8);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 986u, CFCID = 986u, NameID = 12686u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class M01SBlackCat(WorldState ws, Actor primary) : Raid.M01NBlackCat.M01NBlackCat(ws, primary);
