// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN1TrinitySeeker;

sealed class MercifulBreeze(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MercifulBreeze, new AOEShapeRect(50f, 2.5f));
sealed class BalefulSwathe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BalefulSwathe, new AOEShapeCone(50f, 90f.Degrees()));
sealed class ActOfMercy(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ActOfMercy, new AOEShapeCross(50f, 4f));
sealed class MercifulBlooms(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MercifulBlooms, 20f);
sealed class MercifulArc(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(12f, 45f.Degrees()), (uint)IconID.MercifulArc, (uint)AID.MercifulArc, tankbuster: true);

sealed class BurningChains(ModuleBase module) : Components.Chains(module, (uint)TetherID.BurningChains, (uint)AID.ScorchingShackle);
sealed class IronImpact(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.IronImpactMarker, (uint)AID.IronImpact, 5.9d);

sealed class DeadIron(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(50f, 15f.Degrees()), (uint)IconID.DeadIron, (uint)AID.DeadIronAOE, 4.6d);

[ModuleInfo(CFCID = 760u, NameID = 9834u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class DRN1TrinitySeeker(WorldState ws, Actor primary) : TrinitySeeker(ws, primary);
