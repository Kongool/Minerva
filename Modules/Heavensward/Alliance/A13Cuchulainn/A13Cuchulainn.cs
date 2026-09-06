// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A13Cuchulainn;

class CorrosiveBile1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CorrosiveBile1, new AOEShapeCone(25, 45.Degrees()));
class FlailingTentacles2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FlailingTentacles2, new AOEShapeRect(32.5f, 3.5f));
class Beckon(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Beckon, new AOEShapeCone(36.875f, 30.Degrees()));
class BileBelow(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BileBelow);
class Pestilence(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Pestilence);
class BlackLung(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BlackLung);
class GrandCorruption(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.GrandCorruption);
class FlailingTentacles2Knockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.FlailingTentacles2, 30, stopAtWall: true);

[ModuleInfo(CFCID = 120u, NameID = 4626u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A13Cuchulainn(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(288, 138.5f), new ArenaBoundsCircle(29.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Foobar));
    }
}