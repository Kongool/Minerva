// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A23Headstone;

class TremblingEpigraph(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TremblingEpigraph);
class FlaringEpigraph(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FlaringEpigraph);
class BigBurst(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.BigBurst);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 168u, CFCID = 168u, NameID = 4868u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
[SkipLocalsInit]
public sealed class A23Headstone : ModuleBase
{
    public A23Headstone(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private A23Headstone(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    private static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var am60 = -60f.Degrees();
        var arena = new ArenaBoundsCustom([new Polygon(new(-184.52202f, 197.28719f), 16f, 32, am60),
        new Polygon(new(-168.522f, 225f), 20f, 32, am60), new Polygon(new(-152.522f, 252.7128f), 16f, 32, am60)],
        [new Rectangle(new(-185.9566f, 235.09061f), 9f, 0.75f, am60), new Rectangle(new(-150.99809f, 214.88989f), 9f, 0.75f, am60)], AdjustForHitboxInwards: true);
        return (arena.Center, arena);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Parthenope));
        Arena.Actors(Enemies((uint)OID.VoidFire));
    }
}
