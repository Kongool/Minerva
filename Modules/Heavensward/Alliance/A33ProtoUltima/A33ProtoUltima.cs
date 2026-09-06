// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A33ProtoUltima;

class AetherialPool(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.AetherialPool, 40, kind: Kind.TowardsOrigin, stopAtWall: true);
class AetherochemicalFlare(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AetherochemicalFlare);

abstract class AetherochemicalLaser(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(71, 4));
class AetherochemicalLaser1(ModuleBase module) : AetherochemicalLaser(module, (uint)AID.AetherochemicalLaser1);
class AetherochemicalLaser2(ModuleBase module) : AetherochemicalLaser(module, (uint)AID.AetherochemicalLaser2);
class AetherochemicalLaser3(ModuleBase module) : AetherochemicalLaser(module, (uint)AID.AetherochemicalLaser3);

class CitadelBuster2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CitadelBuster2, new AOEShapeRect(65.5f, 5));
class FlareStar(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlareStar, 31);

class Rotoswipe(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rotoswipe, new AOEShapeCone(11, 60.Degrees()));

class WreckingBall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WreckingBall, 8);

[ModuleInfo(CFCID = 220u, NameID = 3780u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A33ProtoUltima(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-350, -50), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.AllaganDreadnaught));
        Arena.Actors(Enemies((uint)OID.AetherCollector));
    }
}
