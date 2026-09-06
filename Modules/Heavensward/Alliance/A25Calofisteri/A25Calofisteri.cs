// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A25Calofisteri;

class AuraBurst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.AuraBurst);
class DepthCharge(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.DepthCharge, 5);
class Extension2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Extension2, 6);
class FeintParticleBeam1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FeintParticleBeam1, 3);
class Penetration(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Penetration, 50, kind: Kind.TowardsOrigin);
class Graft(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Graft, 5);

abstract class Haircut(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(65.5f, 90.Degrees()));
class Haircut1(ModuleBase module) : Haircut(module, (uint)AID.Haircut1);
class Haircut2(ModuleBase module) : Haircut(module, (uint)AID.Haircut2);

abstract class SplitEnd(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(25.5f, 22.5f.Degrees()));
class SplitEnd1(ModuleBase module) : SplitEnd(module, (uint)AID.SplitEnd1);
class SplitEnd2(ModuleBase module) : SplitEnd(module, (uint)AID.SplitEnd2);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 168u, CFCID = 168u, NameID = 4897u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
[SkipLocalsInit]
public sealed class A25Calofisteri : ModuleBase
{
    public A25Calofisteri(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private A25Calofisteri(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(-300f, -34f), 29.5f, 96)], [new Rectangle(new(-300f, -4.26167f), 16.5f, 1.25f)]);
        return (arena.Center, arena);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Bijou1));
        Arena.Actors(Enemies((uint)OID.Bijou2));
        Arena.Actors(Enemies((uint)OID.GrandBijou));
        Arena.Actors(Enemies((uint)OID.LivingLock1));
        Arena.Actors(Enemies((uint)OID.LivingLock2));
        Arena.Actors(Enemies((uint)OID.LivingLock3));
        Arena.Actors(Enemies((uint)OID.LurkingLock));
        Arena.Actors(Enemies((uint)OID.Entanglement));
    }
}
