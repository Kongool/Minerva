// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C013Shadowcaster;

abstract class FiresteelFracture(ModuleBase module, uint aid) : Components.Cleave(module, aid, new AOEShapeCone(40f, 30f.Degrees()));
sealed class NFiresteelFracture(ModuleBase module) : FiresteelFracture(module, (uint)AID.NFiresteelFracture);
sealed class SFiresteelFracture(ModuleBase module) : FiresteelFracture(module, (uint)AID.SFiresteelFracture);

abstract class PureFire(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 6f);
sealed class NPureFire(ModuleBase module) : PureFire(module, (uint)AID.NPureFireAOE);
sealed class SPureFire(ModuleBase module) : PureFire(module, (uint)AID.SPureFireAOE);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 878u, CFCID = 878u, NameID = 11393u, PrimaryActorOID = (uint)OID.NBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C013NShadowcaster(WorldState ws, Actor primary) : V1SildihnSubterrane.V14ZelessGah.VCZelessGah(ws, primary);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 879u, CFCID = 879u, NameID = 11393u, PrimaryActorOID = (uint)OID.SBoss, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class C013SShadowcaster(WorldState ws, Actor primary) : V1SildihnSubterrane.V14ZelessGah.VCZelessGah(ws, primary);
