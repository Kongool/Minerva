// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C022Gorai;

abstract class Unenlightenment(ModuleBase module, uint aid) : Components.CastCounter(module, aid);
sealed class NUnenlightenment(ModuleBase module) : Unenlightenment(module, (uint)AID.NUnenlightenmentAOE);
sealed class SUnenlightenment(ModuleBase module) : Unenlightenment(module, (uint)AID.SUnenlightenmentAOE);

public abstract class C022Gorai(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(300f, -120f), new ArenaBoundsSquare(22.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 946u, CFCID = 946u, NameID = 12373u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C022NGorai(WorldState ws, Actor primary) : C022Gorai(ws, primary);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 947u, CFCID = 947u, NameID = 12373u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C022SGorai(WorldState ws, Actor primary) : C022Gorai(ws, primary);
