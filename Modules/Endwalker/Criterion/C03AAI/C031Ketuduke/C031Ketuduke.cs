// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C031Ketuduke;

abstract class TidalRoar(ModuleBase module, uint aid) : Components.CastCounter(module, aid);
class NTidalRoar(ModuleBase module) : TidalRoar(module, (uint)AID.NTidalRoarAOE);
class STidalRoar(ModuleBase module) : TidalRoar(module, (uint)AID.STidalRoarAOE);

abstract class BubbleNet(ModuleBase module, uint aid) : Components.CastCounter(module, aid);
class NBubbleNet1(ModuleBase module) : BubbleNet(module, (uint)AID.NBubbleNet1AOE);
class SBubbleNet1(ModuleBase module) : BubbleNet(module, (uint)AID.SBubbleNet1AOE);
class NBubbleNet2(ModuleBase module) : BubbleNet(module, (uint)AID.NBubbleNet2AOE);
class SBubbleNet2(ModuleBase module) : BubbleNet(module, (uint)AID.SBubbleNet2AOE);

abstract class Hydrobomb(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 5f);
class NHydrobomb(ModuleBase module) : Hydrobomb(module, (uint)AID.NHydrobombAOE);
class SHydrobomb(ModuleBase module) : Hydrobomb(module, (uint)AID.SHydrobombAOE);

public abstract class C031Ketuduke(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsSquare(20f));

[ModuleInfo(CFCID = 979u, NameID = 12605u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C031NKetuduke(WorldState ws, Actor primary) : C031Ketuduke(ws, primary);

[ModuleInfo(CFCID = 980u, NameID = 12605u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C031SKetuduke(WorldState ws, Actor primary) : C031Ketuduke(ws, primary);
