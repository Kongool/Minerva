// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M05SDancingGreen;

sealed class EighthBeats(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.EighthBeats, 5f);
sealed class QuarterBeats(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.QuarterBeats, 4f, 2, 2);
sealed class DoTheHustle(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.DoTheHustle1, (uint)AID.DoTheHustle2,
(uint)AID.DoTheHustle3, (uint)AID.DoTheHustle4], new AOEShapeCone(50f, 90f.Degrees()));
sealed class Moonburn(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.Moonburn1, (uint)AID.Moonburn2], new AOEShapeRect(40f, 7.5f));

sealed class DeepCut(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(60f, 22.5f.Degrees()), (uint)IconID.DeepCut, (uint)AID.DeepCut, 5.7f, tankbuster: true);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 1020u, CFCID = 1020u, NameID = 13778u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public sealed class M05SDancingGreen(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsSquare(20f));
