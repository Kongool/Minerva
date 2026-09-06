// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A14Phlegethon;

class MegiddoFlame2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MegiddoFlame2, 3);
class MegiddoFlame3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MegiddoFlame3, 4);
class MegiddoFlame4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MegiddoFlame4, 5);
class MegiddoFlame5(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MegiddoFlame5, 6);
class MoonfallSlash(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MoonfallSlash, new AOEShapeCone(15, 60.Degrees()));
class VacuumSlash2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VacuumSlash2, new AOEShapeCone(80, 22.5f.Degrees()));
class AncientFlare1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AncientFlare1, 35);

[ModuleInfo(CFCID = 92u, NameID = 732u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A14Phlegethon(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-110, 180), new ArenaBoundsCircle(35))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.IronClaws));
        Arena.Actors(Enemies((uint)OID.IronGiant));
    }
}
