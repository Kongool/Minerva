// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A21ArachneEve;

class DarkSpike(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.DarkSpike);
class SilkenSpray(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SilkenSpray, new AOEShapeCone(24.5f, 30f.Degrees()));
class ShadowBurst(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.ShadowBurst, 6, 8);
class SpiderThread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.SpiderThread, 6);
sealed class Pitfall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Pitfall1, 20f);
class FrondAffeared(ModuleBase module) : Components.CastGaze(module, (uint)AID.FrondAffeared);
class TheWidowsEmbrace(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.TheWidowsEmbrace, 18f, kind: Kind.TowardsOrigin, stopAtWall: true);
class TheWidowsKiss(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.TheWidowsKiss, 4, kind: Kind.TowardsOrigin, stopAtWall: true);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 168u, CFCID = 168u, NameID = 4871u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
[SkipLocalsInit]
public sealed class A21ArachneEve : ModuleBase
{
    public A21ArachneEve(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private A21ArachneEve(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(16f, -55f), 29.1f, 96)],
        [new Rectangle(new(16f, -84.5f), 22.9f, 1.25f), new Rectangle(new(16f, -25.6f), 22.9f, 1.25f)]);
        return (arena.Center, arena);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Keyknot));
        Arena.Actors(Enemies((uint)OID.Webmaiden));
        Arena.Actors(Enemies((uint)OID.SpittingSpider));
        Arena.Actors(Enemies((uint)OID.SkitteringSpider));
        Arena.Actors(Enemies((uint)OID.EarthAether));
        Arena.Actors(Enemies((uint)OID.DeepEarthAether));
    }
}
