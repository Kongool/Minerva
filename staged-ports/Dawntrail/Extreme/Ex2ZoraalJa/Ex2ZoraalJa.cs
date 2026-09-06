// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex2ZoraalJa;

sealed class MultidirectionalDivide(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MultidirectionalDivide, new AOEShapeCross(30f, 2f));
sealed class MultidirectionalDivideMain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MultidirectionalDivideMain, new AOEShapeCross(30f, 4f));
sealed class MultidirectionalDivideExtra(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MultidirectionalDivideExtra, new AOEShapeCross(40f, 2f));
sealed class RegicidalRage(ModuleBase module) : Components.TankbusterTether(module, (uint)AID.RegicidalRageAOE, (uint)TetherID.RegicidalRage, 8f);
sealed class BitterWhirlwind(ModuleBase module) : Components.TankSwap(module, (uint)AID.BitterWhirlwind, (uint)AID.BitterWhirlwindAOEFirst, (uint)AID.BitterWhirlwindAOERest, default, 3.1d, 5f, true);
sealed class BurningChains(ModuleBase module) : Components.Chains(module, (uint)TetherID.BurningChains, (uint)AID.BurningChainsAOE);
sealed class HalfCircuitRect(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HalfCircuitAOERect, new AOEShapeRect(60f, 60f));
sealed class HalfCircuitDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HalfCircuitAOEDonut, new AOEShapeDonut(10f, 30f));
sealed class HalfCircuitCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HalfCircuitAOECircle, 10f);
sealed class DutysEdge(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.DutysEdgeTarget, (uint)AID.DutysEdgeAOE, 5.3d, 100f, 4f, 8, 8, 4, false);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 996u, CFCID = 996u, NameID = 12882u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Ex2ZoraalJa(WorldState ws, Actor primary) : Trial.T02ZoraalJa.ZoraalJa(ws, primary);