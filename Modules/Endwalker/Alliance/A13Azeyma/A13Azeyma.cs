// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A13Azeyma;

sealed class WardensWarmth(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.WardensWarmthAOE, 6f, tankbuster: true);
sealed class FleetingSpark(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FleetingSpark, new AOEShapeCone(60f, 135f.Degrees()));
sealed class SolarFold(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SolarFoldAOE, new AOEShapeCross(30f, 5f));
sealed class Sunbeam(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Sunbeam, 9f, 14);
sealed class SublimeSunset(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SublimeSunsetAOE, 40f); // TODO: check falloff

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 866u, CFCID = 866u, NameID = 11277u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class A13Azeyma : ModuleBase
{
    public A13Azeyma(WorldState ws, Actor primary) : this(ws, primary, BuildArena()) { }

    private A13Azeyma(WorldState ws, Actor primary, (WPos center, ArenaBoundsCustom arena) a) : base(ws, primary, a.center, a.arena) { }

    public static (WPos center, ArenaBoundsCustom arena) BuildArena()
    {
        var arena = new ArenaBoundsCustom([new Polygon(new(-750f, -750f), 29.5f, 180)], [new Rectangle(new(-750f, -719.981f), 20f, 1.25f),
        new Rectangle(new(-750f, -779.985f), 20f, 1.25f)]);
        return (arena.Center, arena);
    }
}
