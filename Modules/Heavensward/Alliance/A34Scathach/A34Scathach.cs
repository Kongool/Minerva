// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A34Scathach;

class ThirtyCries(ModuleBase module) : Components.Cleave(module, (uint)AID.ThirtyCries, new AOEShapeCircle(12));
class ThirtyThorns4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThirtyThorns4, 8);
class ThirtySouls(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ThirtySouls);
class ThirtyArrows2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThirtyArrows2, new AOEShapeRect(35.5f, 4));
class ThirtyArrows1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ThirtyArrows1, 8);
class TheDragonsVoice(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TheDragonsVoice, 30);

class Shadespin2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shadespin2, new AOEShapeCone(30, 45.Degrees()));

class Shadesmite1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shadesmite1, 15);
class Shadesmite2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shadesmite2, 3);
class Shadesmite3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shadesmite3, 3);

class Pitfall(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Pitfall);
class FullSwing(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FullSwing);

class Nox(ModuleBase module) : Components.StandardChasingAOEs(module, 10f, (uint)AID.NoxAOEFirst, (uint)AID.NoxAOERest, 5.5f, 1.6d, 5, true, (uint)IconID.Nox);

abstract class MarrowDrain(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(10.44f, 60.Degrees()));
class MarrowDrain1(ModuleBase module) : MarrowDrain(module, (uint)AID.MarrowDrain1);
class MarrowDrain2(ModuleBase module) : MarrowDrain(module, (uint)AID.MarrowDrain2);

class BigHug(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BigHug, new AOEShapeRect(5.25f, 1.5f));

[ModuleInfo(CFCID = 220u, NameID = 5515u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A34Scathach(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, -50), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Connla));
        Arena.Actors(Enemies((uint)OID.Connla2));
        Arena.Actors(Enemies((uint)OID.ShadowLimb));
        Arena.Actors(Enemies((uint)OID.ShadowcourtJester));
        Arena.Actors(Enemies((uint)OID.ChimeraPoppet));
        Arena.Actors(Enemies((uint)OID.ShadowcourtHound));
    }
}
