// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A32FiveheadedDragon;

class WhiteBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhiteBreath, new AOEShapeCone(30, 60.Degrees()));
class BreathOfFire(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BreathOfFire, 6);
class BreathOfLight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BreathOfLight, 6);
class BreathOfPoison(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BreathOfPoison, 6);
class BreathOfIce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BreathOfIce, 6);

class Radiance(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Radiance);
class HeatWave(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HeatWave);

[ModuleInfo(CFCID = 111u, NameID = 3227u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A32FiveheadedDragon(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(200, 180), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.HeadOfFire));
        Arena.Actors(Enemies((uint)OID.HeadOfPoison));
        Arena.Actors(Enemies((uint)OID.HeadOfThunder));
        Arena.Actors(Enemies((uint)OID.HeadOfIce));
        Arena.Actors(Enemies((uint)OID.Prominence));
        Arena.Actors(Enemies((uint)OID.PoisonSlime));
        Arena.Actors(Enemies((uint)OID.ToxicSlime));
        Arena.Actors(Enemies((uint)OID.DragonfireFly));
    }
}
