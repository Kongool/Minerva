// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M04NWickedThunder;

sealed class WickedJolt(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.WickedJolt, new AOEShapeRect(60f, 2.5f), endsOnCastEvent: true, tankbuster: true);

sealed class WickedBolt(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.WickedBolt, (uint)AID.WickedBolt, 5f, 5f, 8, 8, 5);
sealed class SoaringSoulpress(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.SoaringSoulpress, (uint)AID.SoaringSoulpress, 6f, 5.4f, 8, 8);
sealed class WrathOfZeus(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.WrathOfZeus);
sealed class BewitchingFlight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BewitchingFlight, new AOEShapeRect(40f, 2.5f));
sealed class Thunderslam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Thunderslam, 5f);
sealed class Thunderstorm(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Thunderstorm, 6f);

[ModuleInfo(CFCID = 991u, NameID = 13057u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public class M04NWickedThunder(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaChanges.DefaultCenter, ArenaChanges.DefaultBounds);
