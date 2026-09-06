// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS1TrinitySeeker;

sealed class VerdantTempest(ModuleBase module) : Components.CastCounter(module, (uint)AID.VerdantTempestAOE);
sealed class MercifulBreeze(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MercifulBreeze, new AOEShapeRect(50f, 2.5f));
sealed class MercifulBlooms(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MercifulBlooms, 20f);
sealed class MercifulArc(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(12f, 45f.Degrees()), (uint)IconID.MercifulArc, (uint)AID.MercifulArc);

// TODO: depending on phantom edge, it's either a shared tankbuster cleave or a weird cleave ignoring closest target (?)
abstract class BalefulOnslaught(ModuleBase module, uint aid) : Components.Cleave(module, aid, new AOEShapeCone(10f, 45f.Degrees())); // TODO: verify angle
sealed class BalefulOnslaught1(ModuleBase module) : BalefulOnslaught(module, (uint)AID.BalefulOnslaughtAOE1);
sealed class BalefulOnslaught2(ModuleBase module) : BalefulOnslaught(module, (uint)AID.BalefulOnslaughtAOE2);

sealed class BurningChains(ModuleBase module) : Components.Chains(module, (uint)TetherID.BurningChains, (uint)AID.ScorchingShackle);

// TODO: it's a line stack, but I don't think there's a way to determine cast target - so everyone should just stack?..
sealed class IronImpact(ModuleBase module) : Components.CastCounter(module, (uint)AID.IronImpact);
sealed class IronRose(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IronRose, new AOEShapeRect(50f, 4f));

sealed class DeadIron(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(50f, 15f.Degrees()), (uint)IconID.DeadIron, (uint)AID.DeadIronAOE, 4.6d);

[ModuleInfo(CFCID = 761u, NameID = 9834u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public sealed class DRS1TrinitySeeker(WorldState ws, Actor primary) : TrinitySeeker(ws, primary);
