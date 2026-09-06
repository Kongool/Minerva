// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A23Construct7;

class Destroy1(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Destroy1);
class Destroy2(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Destroy2);
class Accelerate(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Accelerate, 6);
class Compress1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Compress1, new AOEShapeRect(104.5f, 3.5f));
class Compress2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Compress2, new AOEShapeCross(100, 3.5f));

class Pulverize2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pulverize2, 12);
class Dispose1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Dispose1, new AOEShapeCone(100, 45.Degrees()));
class Dispose3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Dispose3, new AOEShapeCone(100, 45.Degrees()));

[ModuleInfo(CFCID = 550u, NameID = 7237u, PrimaryActorOID = (uint)OID.Construct, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A23Construct7(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(800, -141), new ArenaBoundsSquare(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Construct711));
        Arena.Actors(Enemies((uint)OID.Construct712));
        Arena.Actors(Enemies((uint)OID.Construct713));
    }
}
