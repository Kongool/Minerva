// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A33Cerberus;

class TailBlow(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TailBlow, new AOEShapeCone(19, 45.Degrees()));
class Slabber(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Slabber, 8);
class Mini(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Mini, 9);
class SulphurousBreath1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SulphurousBreath1, new AOEShapeRect(35, 3));
class SulphurousBreath2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SulphurousBreath2, new AOEShapeRect(45, 3));
class LightningBolt2(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.LightningBolt2, 2);
class HoundOutOfHell(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.HoundOutOfHell, 7);
class Ululation(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Ululation);

[ModuleInfo(CFCID = 111u, NameID = 3224u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A33Cerberus(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, -197), new ArenaBoundsRect(20, 40))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.GastricJuice));
        Arena.Actors(Enemies((uint)OID.StomachWall));
        Arena.Actors(Enemies((uint)OID.Wolfsbane));
    }
}
