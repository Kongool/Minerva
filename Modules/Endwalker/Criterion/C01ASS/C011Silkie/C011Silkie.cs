// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C011Silkie;

abstract class FizzlingDuster(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 22.5f.Degrees()));
sealed class NFizzlingDuster(ModuleBase module) : FizzlingDuster(module, (uint)AID.NFizzlingDusterAOE);
sealed class SFizzlingDuster(ModuleBase module) : FizzlingDuster(module, (uint)AID.SFizzlingDusterAOE);
sealed class NFizzlingDusterPuff(ModuleBase module) : FizzlingDuster(module, (uint)AID.NFizzlingDusterPuff);
sealed class SFizzlingDusterPuff(ModuleBase module) : FizzlingDuster(module, (uint)AID.SFizzlingDusterPuff);

abstract class DustBluster(ModuleBase module, uint aid) : Components.SimpleKnockbacks(module, aid, 16f);
sealed class NDustBluster(ModuleBase module) : DustBluster(module, (uint)AID.NDustBluster);
sealed class SDustBluster(ModuleBase module) : DustBluster(module, (uint)AID.SDustBluster);

abstract class SqueakyCleanE(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 112.5f.Degrees()));
sealed class NSqueakyCleanE(ModuleBase module) : SqueakyCleanE(module, (uint)AID.NSqueakyCleanAOE3E);
sealed class SSqueakyCleanE(ModuleBase module) : SqueakyCleanE(module, (uint)AID.SSqueakyCleanAOE3E);

abstract class SqueakyCleanW(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 112.5f.Degrees()));
sealed class NSqueakyCleanW(ModuleBase module) : SqueakyCleanW(module, (uint)AID.NSqueakyCleanAOE3W);
sealed class SSqueakyCleanW(ModuleBase module) : SqueakyCleanW(module, (uint)AID.SSqueakyCleanAOE3W);

abstract class ChillingDusterPuff(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCross(60f, 5f));
sealed class NChillingDusterPuff(ModuleBase module) : ChillingDusterPuff(module, (uint)AID.NChillingDusterPuff);
sealed class SChillingDusterPuff(ModuleBase module) : ChillingDusterPuff(module, (uint)AID.SChillingDusterPuff);

abstract class BracingDusterPuff(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(5f, 60f));
sealed class NBracingDusterPuff(ModuleBase module) : BracingDusterPuff(module, (uint)AID.NBracingDusterPuff);
sealed class SBracingDusterPuff(ModuleBase module) : BracingDusterPuff(module, (uint)AID.SBracingDusterPuff);

public abstract class C011Silkie(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-335f, -155f), new ArenaBoundsSquare(29.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 878u, CFCID = 878u, NameID = 11369u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class C011NSilkie(WorldState ws, Actor primary) : C011Silkie(ws, primary);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 879u, CFCID = 879u, NameID = 11369u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class C011SSilkie(WorldState ws, Actor primary) : C011Silkie(ws, primary);
