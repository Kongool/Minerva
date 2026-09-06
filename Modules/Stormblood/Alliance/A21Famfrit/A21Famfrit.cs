// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A21Famfrit;

class TidePod(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.TidePod);
class WaterIV(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.WaterIV);
class DarkeningDeluge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkeningDeluge, 6);
class Tsunami9(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Tsunami9, 15, stopAtWall: true, kind: Kind.AwayFromOrigin);
class Materialize(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Materialize, 6);
class DarkRain2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkRain2, 8);

[ModuleInfo(CFCID = 550u, NameID = 7245u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A21Famfrit(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-200, 66.5f), new ArenaBoundsCircle(30));
