// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex6Golbez;

class Terrastorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TerrastormAOE, 16f);
class LingeringSpark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LingeringSparkAOE, 5f);

abstract class PhasesOfTheBlade(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(22f, 90f.Degrees()));
class PhasesOfTheBladeFront(ModuleBase module) : PhasesOfTheBlade(module, (uint)AID.PhasesOfTheBlade);
class PhasesOfTheBladeBack(ModuleBase module) : PhasesOfTheBlade(module, (uint)AID.PhasesOfTheBladeBack);
class PhasesOfTheShadowFront(ModuleBase module) : PhasesOfTheBlade(module, (uint)AID.PhasesOfTheShadow);
class PhasesOfTheShadowBack(ModuleBase module) : PhasesOfTheBlade(module, (uint)AID.PhasesOfTheShadowBack);

class ArcticAssault(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ArcticAssaultAOE, new AOEShapeRect(15f, 7.5f));
class RisingBeacon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RisingBeaconAOE, 10f);
class RisingRing(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RisingRingAOE, new AOEShapeDonut(6f, 22f));
class BurningShade(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.BurningShade, 5f);
class ImmolatingShade(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.ImmolatingShade, 6f, 4, 4);
class VoidBlizzard(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.VoidBlizzard, 6f, 4, 4);
class VoidAero(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.VoidAero, 3f, 2, 2);
class VoidTornado(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.VoidTornado, 6f, 4, 4);

[ModuleInfo(CFCID = 950u, NameID = 12365u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex6Golbez(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(15f));
