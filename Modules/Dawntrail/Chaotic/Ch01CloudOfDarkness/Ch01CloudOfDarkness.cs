// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class Flare(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Flare, (uint)AID.FlareAOE, 25f, 8.1d);
sealed class StygianShadow(ModuleBase module) : Components.Adds(module, (uint)OID.StygianShadow);
sealed class Atomos(ModuleBase module) : Components.Adds(module, (uint)OID.Atomos);
sealed class GhastlyGloomCross(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GhastlyGloomCrossAOE, new AOEShapeCross(40f, 15f));
sealed class GhastlyGloomDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GhastlyGloomDonutAOE, new AOEShapeDonut(21f, 40f));
sealed class FloodOfDarknessAdd(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.FloodOfDarknessAdd); // TODO: only if add is player's?..
sealed class Excruciate(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.Excruciate, 4f, tankbuster: true);
sealed class LoomingChaos(ModuleBase module) : Components.CastCounter(module, (uint)AID.LoomingChaosAOE);
sealed class Phaser(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.Phaser], new AOEShapeCone(23f, 30f.Degrees()));
sealed class FeintParticleBeam(ModuleBase module) : Components.StandardChasingAOEs(module, 3f, (uint)AID.FeintParticleBeamAOEFirst, (uint)AID.FeintParticleBeamAOERest, 2.1f, 0.4d, 18, icon: (uint)IconID.FeintParticleBeam);

// TODO: tankswap hints component for phase1
// TODO: phase 2 teleport zones?
// TODO: grim embrace / curse of darkness prevent turning

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1010u, CFCID = 1010u, NameID = 13624u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class Ch01CloudOfDarkness(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsCircle(40f));
