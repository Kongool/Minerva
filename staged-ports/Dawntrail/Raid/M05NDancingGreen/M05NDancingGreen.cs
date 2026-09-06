// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M05NDancingGreen;

sealed class DoTheHustle(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.DoTheHustle1, (uint)AID.DoTheHustle2], new AOEShapeCone(50f, 90f.Degrees()));
sealed class DeepCut(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(60f, 22.5f.Degrees()), (uint)IconID.DeepCut, (uint)AID.DeepCut, 5f, tankbuster: true);
sealed class FullBeat(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.FullBeat, 6f, 8, 8);
sealed class CelebrateGoodTimesDiscoInfernalLetsPose(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.CelebrateGoodTimes, (uint)AID.DiscoInfernal,
(uint)AID.LetsPose1, (uint)AID.LetsPose2]);
sealed class EighthBeats(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.EighthBeats, 5f);
sealed class Moonburn(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Moonburn1, (uint)AID.Moonburn2], new AOEShapeRect(40f, 7.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1019u, CFCID = 1019u, NameID = 13778u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M05NDancingGreen(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f));
