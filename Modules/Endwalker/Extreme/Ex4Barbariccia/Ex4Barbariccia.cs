// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex4Barbariccia;

class RagingStorm(ModuleBase module) : Components.CastCounter(module, (uint)AID.RagingStorm);
class HairFlayUpbraid(ModuleBase module) : Components.CastStackSpread(module, (uint)AID.Upbraid, (uint)AID.HairFlay, 3, 10, maxStackSize: 2);
class CurlingIron(ModuleBase module) : Components.CastCounter(module, (uint)AID.CurlingIronAOE);
class Catabasis(ModuleBase module) : Components.CastCounter(module, (uint)AID.Catabasis);
class VoidAeroTankbuster(ModuleBase module) : Components.Cleave(module, (uint)AID.VoidAeroTankbuster, new AOEShapeCircle(5), originAtTarget: true);
class SecretBreezeCones(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SecretBreezeAOE, new AOEShapeCone(40, 22.5f.Degrees()));
class SecretBreezeProteans(ModuleBase module) : Components.SimpleProtean(module, (uint)AID.SecretBreezeProtean, new AOEShapeCone(40, 22.5f.Degrees()));

class WarningGale(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WarningGale, 6);
class WindingGaleCharge(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.WindingGaleCharge, 2);
class BoulderBreak(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.BoulderBreak, 5);
class Boulder(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Boulder, 10);
class BrittleBoulder(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.BrittleBoulder, 5);
class TornadoChainInner(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TornadoChainInner, 11);
class TornadoChainOuter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TornadoChainOuter, new AOEShapeDonut(11, 20));
class KnuckleDrum(ModuleBase module) : Components.CastCounter(module, (uint)AID.KnuckleDrum);
class KnuckleDrumLast(ModuleBase module) : Components.CastCounter(module, (uint)AID.KnuckleDrumLast);
class BlowAwayRaidwide(ModuleBase module) : Components.CastCounter(module, (uint)AID.BlowAwayRaidwide);
class BlowAwayPuddle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlowAwayPuddle, 6);
class ImpactAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ImpactAOE, 6);
class ImpactKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ImpactKnockback, 6);
class BlusteryRuler(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlusteryRuler, 6);
class DryBlowsRaidwide(ModuleBase module) : Components.CastCounter(module, (uint)AID.DryBlowsRaidwide);
class DryBlowsPuddle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DryBlowsPuddle, 3);
class IronOut(ModuleBase module) : Components.CastCounter(module, (uint)AID.IronOutAOE);

[ModuleInfo(CFCID = 871u, NameID = 11398u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Ex4Barbariccia(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100, 100), new ArenaBoundsCircle(20));
