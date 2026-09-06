// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C012Gladiator;

abstract class RushOfMightFront(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 90f.Degrees()));
sealed class NRushOfMightFront(ModuleBase module) : RushOfMightFront(module, (uint)AID.NRushOfMightFront);
sealed class SRushOfMightFront(ModuleBase module) : RushOfMightFront(module, (uint)AID.SRushOfMightFront);

abstract class RushOfMightBack(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60f, 90f.Degrees()));
sealed class NRushOfMightBack(ModuleBase module) : RushOfMightBack(module, (uint)AID.NRushOfMightBack);
sealed class SRushOfMightBack(ModuleBase module) : RushOfMightBack(module, (uint)AID.SRushOfMightBack);

public abstract class C012Gladiator(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-35f, -271f), new ArenaBoundsSquare(19.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 878u, CFCID = 878u, NameID = 11387u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class C012NGladiator(WorldState ws, Actor primary) : C012Gladiator(ws, primary);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 879u, CFCID = 879u, NameID = 11387u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class C012SGladiator(WorldState ws, Actor primary) : C012Gladiator(ws, primary);
