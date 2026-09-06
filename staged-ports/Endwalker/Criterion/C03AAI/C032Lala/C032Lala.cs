// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C032Lala;

abstract class ArcaneBlight(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 135f.Degrees()));
class NArcaneBlight(ModuleBase module) : ArcaneBlight(module, (uint)AID.NArcaneBlightAOE);
class SArcaneBlight(ModuleBase module) : ArcaneBlight(module, (uint)AID.SArcaneBlightAOE);

public abstract class C032Lala(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(200f, default), new ArenaBoundsSquare(20f));

[ModuleInfo(CFCID = 979u, NameID = 12639u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C032NLala(WorldState ws, Actor primary) : C032Lala(ws, primary);

[ModuleInfo(CFCID = 980u, NameID = 12639u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class C032SLala(WorldState ws, Actor primary) : C032Lala(ws, primary);
