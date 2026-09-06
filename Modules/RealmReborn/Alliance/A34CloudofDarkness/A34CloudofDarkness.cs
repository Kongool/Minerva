// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A34CloudofDarkness;

class ZeroFormParticleBeam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ZeroFormParticleBeam, new AOEShapeRect(74, 12));
class ParticleBeam2(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ParticleBeam2);

class FeintParticleBeam(ModuleBase module) : Components.StandardChasingAOEs(module, 10f, (uint)AID.FeintParticleBeam1, (uint)AID.FeintParticleBeam2, 4, 1.5f, 5, true, (uint)IconID.FeintParticleBeam)
{
}

[ModuleInfo(CFCID = 111u, NameID = 3240u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A34CloudofDarkness(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-300, -400), new ArenaBoundsCircle(30))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.DarkCloud));
        Arena.Actors(Enemies((uint)OID.DarkStorm));
        Arena.Actors(Enemies((uint)OID.HyperchargedCloud));
    }
}
