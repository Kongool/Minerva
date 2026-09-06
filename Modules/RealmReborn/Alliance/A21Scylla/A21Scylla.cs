// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A21Scylla;

class Topple(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Topple, new AOEShapeCircle(6.75f));
class SearingChain(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SearingChain, new AOEShapeRect(61, 2));
class InfiniteAnguish(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.InfiniteAnguish, new AOEShapeDonut(6, 12));
class AncientFlare(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AncientFlare);

[ModuleInfo(CFCID = 102u, NameID = 2809u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A21Scylla(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(0, -192), new ArenaBoundsCircle(35))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.StaffOfEldering));
        Arena.Actors(Enemies((uint)OID.ShudderingSoul));
        Arena.Actors(Enemies((uint)OID.ShiveringSoul));
        Arena.Actors(Enemies((uint)OID.SmolderingSoul));
    }
}
