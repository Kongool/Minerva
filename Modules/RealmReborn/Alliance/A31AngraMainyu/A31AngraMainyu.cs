// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A31AngraMainyu;

class DoubleVision(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DoubleVision);
class MortalGaze1(ModuleBase module) : Components.CastGaze(module, (uint)AID.MortalGaze1);
class Level100Flare1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Level100Flare1, 10);
class Level150Death1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Level150Death1, 10);

[ModuleInfo(CFCID = 111u, NameID = 3231u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A31AngraMainyu(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-145, 300), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.FinalHourglass));
        Arena.Actors(Enemies((uint)OID.GrimReaper));
        Arena.Actors(Enemies((uint)OID.AngraMainyusDaewa));
    }
}
