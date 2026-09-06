// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A11BoneDragon;

class Apocalypse(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Apocalypse, 6);
class EvilEye(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EvilEye, new AOEShapeCone(105, 60.Degrees()));
class Stone(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Stone);
class Level5Petrify(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Level5Petrify, new AOEShapeCone(7.8f, 45.Degrees()));

[ModuleInfo(CFCID = 92u, NameID = 706u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A11BoneDragon(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Circle(new(-450, 30), 15), new Rectangle(new(-450, 0), 10, 20)]);
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Platinal));
        Arena.Actors(Enemies((uint)OID.RottingEye));
    }
}
