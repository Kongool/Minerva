// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C021Shishio;

abstract class SplittingCry(ModuleBase module, uint aid) : Components.BaitAwayCast(module, aid, new AOEShapeRect(60f, 7f));
sealed class NSplittingCry(ModuleBase module) : SplittingCry(module, (uint)AID.NSplittingCry);
sealed class SSplittingCry(ModuleBase module) : SplittingCry(module, (uint)AID.SSplittingCry);

abstract class ThunderVortex(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeDonut(8f, 30f));
sealed class NThunderVortex(ModuleBase module) : ThunderVortex(module, (uint)AID.NThunderVortex);
sealed class SThunderVortex(ModuleBase module) : ThunderVortex(module, (uint)AID.SThunderVortex);

public abstract class C021Shishio(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0f, -100f), new ArenaBoundsSquare(24.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 946u, CFCID = 946u, NameID = 12428u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C021NShishio(WorldState ws, Actor primary) : C021Shishio(ws, primary);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 947u, CFCID = 947u, NameID = 12428u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C021SShishio(WorldState ws, Actor primary) : C021Shishio(ws, primary);
