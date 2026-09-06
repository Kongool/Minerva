// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A24Xande;

class KnucklePress(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.KnucklePress, 10);
class BurningRave1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BurningRave1, 8);
class BurningRave2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BurningRave2, 8);
class AncientQuake(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AncientQuake);
class AncientQuaga(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AncientQuaga);
class AuraCannon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AuraCannon, new AOEShapeRect(60, 5));
//class Stackmarker(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.Stackmarker, (uint)AID.KnucklePress, 6, 2, 4);

[ModuleInfo(CFCID = 102u, NameID = 2824u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A24Xande(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-400, -200), new ArenaBoundsCircle(35))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.StonefallCircle));
        Arena.Actors(Enemies((uint)OID.StarfallCircle));
    }
}
