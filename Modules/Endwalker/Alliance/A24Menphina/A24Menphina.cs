// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A24Menphina;

class BlueMoon(ModuleBase module) : Components.CastCounter(module, (uint)AID.BlueMoonAOE);
class FirstBlush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FirstBlush, new AOEShapeRect(80, 12.5f));
class SilverMirror(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SilverMirrorAOE, 7);
class Moonset(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MoonsetAOE, 12);

class LoversBridge(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 19);
class LoversBridgeShort(ModuleBase module) : LoversBridge(module, (uint)AID.LoversBridgeShort);
class LoversBridgeLong(ModuleBase module) : LoversBridge(module, (uint)AID.LoversBridgeLong);

class CeremonialPillar(ModuleBase module) : Components.Adds(module, (uint)OID.CeremonialPillar);
class AncientBlizzard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientBlizzard, new AOEShapeCone(45, 22.5f.Degrees()));
class KeenMoonbeam(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.KeenMoonbeamAOE, 6);
class RiseOfTheTwinMoons(ModuleBase module) : Components.CastCounter(module, (uint)AID.RiseOfTheTwinMoons);
class CrateringChill(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CrateringChillAOE, 20);
class MoonsetRays(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.MoonsetRaysAOE, 6, 4);

[ModuleInfo(CFCID = 911u, NameID = 12063u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class A24Menphina(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(800, 750), new ArenaBoundsCircle(25));
