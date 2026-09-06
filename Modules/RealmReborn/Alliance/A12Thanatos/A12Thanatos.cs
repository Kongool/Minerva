// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A12Thanatos;

class BlackCloud(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlackCloud, 6);
class Cloudscourge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Cloudscourge, 6);
class VoidFireII(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VoidFireII, 5);
class AstralLight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AstralLight, 6.8f);

[ModuleInfo(CFCID = 92u, NameID = 710u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A12Thanatos(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(440, 280), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.MagicPot), Colors.Object);
        Arena.Actors(Enemies((uint)OID.Nemesis));
        Arena.Actors(Enemies((uint)OID.Sandman));
    }
}
