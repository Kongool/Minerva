// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A22Belias;

class FireIV(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FireIV);
class Eruption(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Eruption, 8f);
class TimeBomb2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TimeBomb2, new AOEShapeCone(60f, 45f.Degrees()));
class TimeEruption(ModuleBase module) : Components.SimpleAOEGroupsByTimewindow(module, [(uint)AID.TimeEruptionAOEFirst, (uint)AID.TimeEruptionAOESecond], new AOEShapeRect(20f, 10f), expectedNumCasters: 9);

[ModuleInfo(CFCID = 550u, NameID = 7223u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A22Belias(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-200f, -541f), new ArenaBoundsSquare(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Gigas));
    }
}
