// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C023Moko;

abstract class LateralSlice(ModuleBase module, uint aid) : Components.BaitAwayCast(module, aid, new AOEShapeCone(40f, 45f.Degrees())); // TODO: verify angle
sealed class NLateralSlice(ModuleBase module) : LateralSlice(module, (uint)AID.NLateralSlice);
sealed class SLateralSlice(ModuleBase module) : LateralSlice(module, (uint)AID.SLateralSlice);

public abstract class C023Moko(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-200f, 0f), new ArenaBoundsSquare(24.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 946u, CFCID = 946u, NameID = 12357u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C023NMoko(WorldState ws, Actor primary) : C023Moko(ws, primary);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 947u, CFCID = 947u, NameID = 12357u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C023SMoko(WorldState ws, Actor primary) : C023Moko(ws, primary);
