// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A36DiabolosHollow;

class Shadethrust(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Shadethrust, new AOEShapeRect(43, 2.5f));
class HollowCamisado(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.HollowCamisado);
class HollowNightmare(ModuleBase module) : Components.CastGaze(module, (uint)AID.HollowNightmare);
class HollowOmen1(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HollowOmen1);
class HollowOmen2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HollowOmen2);
class Blindside(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.Blindside, 6, 8);
class EarthShaker2(ModuleBase module) : Components.SimpleProtean(module, (uint)AID.EarthShaker2, new AOEShapeCone(60f, 15f.Degrees()));
class HollowNight(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.HollowNight, 8, 8);
class HollowNightGaze(ModuleBase module) : Components.CastGaze(module, (uint)AID.HollowNight);
class ParticleBeam2(ModuleBase module) : Components.CastTowers(module, (uint)AID.ParticleBeam2, 5);
class ParticleBeam4(ModuleBase module) : Components.CastTowers(module, (uint)AID.ParticleBeam4, 5);

class Nox(ModuleBase module) : Components.StandardChasingAOEs(module, 10f, (uint)AID.NoxAOEFirst, (uint)AID.NoxAOERest, 5.5f, 1.6f, 5, true, (uint)IconID.Nox)
{
}

[ModuleInfo(CFCID = 220u, NameID = 5526u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A36DiabolosHollow(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-350, -445), new ArenaBoundsCircle(35))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Deathgate));
        Arena.Actors(Enemies((uint)OID.DiabolicGate));
        Arena.Actors(Enemies((uint)OID.Shadowsphere));
        Arena.Actors(Enemies((uint)OID.NightHound));
    }
}
