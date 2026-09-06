// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A24Yiazmat;

class RakeTB(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.RakeTB);
class RakeSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.RakeSpread, 5);
class RakeAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RakeAOE, 10);
class RakeLoc1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RakeLoc1, 10);
class RakeLoc2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RakeLoc2, 10);
class StoneBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.StoneBreath, new AOEShapeCone(60, 22.5f.Degrees()));
class DustStorm2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DustStorm2);
class WhiteBreath(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WhiteBreath, new AOEShapeDonut(10, 60));

class AncientAero(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientAero, new AOEShapeRect(40, 3));
class Karma(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Karma, new AOEShapeCone(30, 45.Degrees()));
class UnholyDarkness(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UnholyDarkness, 8);

class SolarStorm1(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.SolarStorm1);

[ModuleInfo(CFCID = 550u, NameID = 7070u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A24Yiazmat(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(800, -400), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.HeartOfTheDragon));
        Arena.Actors(Enemies((uint)OID.Archaeodemon));
        Arena.Actors(Enemies((uint)OID.WindAzer));
    }
}
