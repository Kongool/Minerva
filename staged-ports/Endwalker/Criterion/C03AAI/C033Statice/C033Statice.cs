// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C033Statice;

abstract class SurpriseBalloon(ModuleBase module, uint aid) : Components.SimpleKnockbacks(module, aid, 13f);
class NSurpriseBalloon(ModuleBase module) : SurpriseBalloon(module, (uint)AID.NPop);
class SSurpriseBalloon(ModuleBase module) : SurpriseBalloon(module, (uint)AID.SPop);

class BeguilingGlitter(ModuleBase module) : Components.StatusDrivenForcedMarch(module, 2f, (uint)SID.ForwardMarch, (uint)SID.AboutFace, (uint)SID.LeftFace, (uint)SID.RightFace, activationLimit: 8);

abstract class FaerieRing(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(6f, 12f)); // TODO: verify inner radius
class NFaerieRing(ModuleBase module) : FaerieRing(module, (uint)AID.NFaerieRing);
class SFaerieRing(ModuleBase module) : FaerieRing(module, (uint)AID.SFaerieRing);

public abstract class C033Statice(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-200f, default), new ArenaBoundsCircle(20f));

[ModuleInfo(CFCID = 979u, NameID = 12506u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C033NStatice(WorldState ws, Actor primary) : C033Statice(ws, primary);

[ModuleInfo(CFCID = 980u, NameID = 12506u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C033SStatice(WorldState ws, Actor primary) : C033Statice(ws, primary);
