// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M03SBruteBomber;

sealed class BrutalImpact(ModuleBase module) : Components.CastCounter(module, (uint)AID.BrutalImpactAOE);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 990u, CFCID = 990u, NameID = 13356u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class M03SBruteBomber(WorldState ws, Actor primary) : ModuleBase(ws, primary, arenaCenter, DefaultBounds)
{
    private static readonly WPos arenaCenter = new(100f, 100f);
    public static readonly ArenaBoundsSquare DefaultBounds = new(15f);
    public static readonly ArenaBoundsCustom FuseFieldBounds = new([new Square(arenaCenter, 15f)], [new Polygon(arenaCenter, 5f, 80)]);
}
