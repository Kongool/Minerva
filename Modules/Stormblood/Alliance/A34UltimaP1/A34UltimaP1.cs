// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A34UltimaP1;

class HolyIVBait(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HolyIVBait, 6f);
class HolyIVSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HolyIVSpread, 6f);
class AuralightAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AuralightAOE, 20f);
class AuralightRect(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AuralightRect, new AOEShapeRect(70f, 5f));
class GrandCrossAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandCrossAOE, new AOEShapeCross(60f, 7.5f));
class TimeEruption(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.TimeEruptionAOEFirst, (uint)AID.TimeEruptionAOESecond], new AOEShapeRect(20f, 10f), expectedNumCasters: 9);

class Eruption2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Eruption2, 8f);
class ControlTower2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ControlTower2, 6f);

abstract class ExtremeEdge(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(60f, 18f));
class ExtremeEdge1(ModuleBase module) : ExtremeEdge(module, (uint)AID.ExtremeEdge1);
class ExtremeEdge2(ModuleBase module) : ExtremeEdge(module, (uint)AID.ExtremeEdge2);

class CrushWeapon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CrushWeapon, 6f);
class Searchlight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Searchlight, 6f);
class HallowedBolt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HallowedBolt, 6f);

[ModuleInfo(CFCID = 636u, NameID = 7909u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A34UltimaP1(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600f, -600f), new ArenaBoundsSquare(30f));
