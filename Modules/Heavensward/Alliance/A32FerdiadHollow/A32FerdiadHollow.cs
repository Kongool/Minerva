// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A32FerdiadHollow;

class Blackbolt(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Blackbolt, 6, 8);

class Blackfire2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Blackfire2, 7); // expanding aoe circle

class JestersJig1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.JestersJig1, 9);

class JestersReap(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.JestersReap, new AOEShapeCone(13.4f, 60.Degrees()));
class JestersReward(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.JestersReward, new AOEShapeCone(31.4f, 90.Degrees()));

class JongleursX(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.JongleursX);
class JugglingSphere(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.JugglingSphere, 3);
class JugglingSphere2(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.JugglingSphere2, 3);

class LuckyPierrot1(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.LuckyPierrot1, 2.5f);
class LuckyPierrot2(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.LuckyPierrot2, 2.5f);

class PetrifyingEye(ModuleBase module) : Components.CastGaze(module, (uint)AID.PetrifyingEye);

class Flameflow1(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Flameflow1);
class Flameflow2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Flameflow2);
class Flameflow3(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Flameflow3);

class Unknown4(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Unknown4, 3);
class Unknown6(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.Unknown6, 3);

class AtmosAOE1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AtmosAOE1, 20);
class AtmosAOE2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AtmosAOE2, 20);
class AtmosDonut(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AtmosDonut, new AOEShapeDonut(6, 20));

[ModuleInfo(CFCID = 220u, NameID = 5509u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A32FerdiadHollow(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-350, 225), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.FerdiadsFool));
    }
}
