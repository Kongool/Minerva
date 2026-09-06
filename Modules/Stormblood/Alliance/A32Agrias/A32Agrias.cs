// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A32Agrias;

class DivineLight(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DivineLight);
class NorthswainsStrikeEphemeralKnight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NorthswainsStrikeEphemeralKnight, new AOEShapeRect(60, 3));
class CleansingFlameSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.CleansingFlameSpread, 6);
class HallowedBoltAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HallowedBoltAOE, 15);

[ModuleInfo(CFCID = 636u, NameID = 7916u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A32Agrias(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(600, -54), new ArenaBoundsCircle(30));
