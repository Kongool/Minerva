// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A11Cetus;

class ElectricSwipe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectricSwipe, new AOEShapeCone(25, 30.Degrees()));
class BodySlam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BodySlam, 10);
class Immersion(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Immersion);
class ElectricWhorl(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectricWhorl, new AOEShapeDonut(7, 60));
class ExpulsionAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Expulsion, 14);
class ExpulsionKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Expulsion, 30, stopAtWall: true);
class BiteAndRun(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.BiteAndRun, 2.5f);

[ModuleInfo(CFCID = 120u, NameID = 4613u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A11Cetus(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-288, 0), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.HybodusPup));
        Arena.Actors(Enemies((uint)OID.Hybodus));
        Arena.Actors(Enemies((uint)OID.Hydrosphere));
        Arena.Actors(Enemies((uint)OID.Hydrocore));
    }
}
