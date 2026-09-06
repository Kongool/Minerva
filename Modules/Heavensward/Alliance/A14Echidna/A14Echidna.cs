// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A14Echidna;

class SickleStrike(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.SickleStrike);

abstract class SickleSlash(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(18.5f, 30));
class SickleSlash1(ModuleBase module) : SickleSlash(module, (uint)AID.SickleSlash1);
class SickleSlash2(ModuleBase module) : SickleSlash(module, (uint)AID.SickleSlash2);

class AbyssalReaper(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbyssalReaper, 14);
class AbyssalReaperKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.AbyssalReaper, 5, stopAtWall: true);
class Petrifaction1(ModuleBase module) : Components.CastGaze(module, (uint)AID.Petrifaction1);
class Petrifaction2(ModuleBase module) : Components.CastGaze(module, (uint)AID.Petrifaction2);
class Gehenna(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Gehenna);
class BloodyHarvest(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BloodyHarvest, 12);
class Deathstrike(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Deathstrike, new AOEShapeRect(62, 3));
class FlameWreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlameWreath, 18);
class SerpentineStrike(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SerpentineStrike, 20);

[ModuleInfo(CFCID = 120u, NameID = 4631u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A14Echidna(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(288, -126), new ArenaBoundsCircle(29.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Dexter));
        Arena.Actors(Enemies((uint)OID.Sinister));
        Arena.Actors(Enemies((uint)OID.Bloodguard));
    }
}