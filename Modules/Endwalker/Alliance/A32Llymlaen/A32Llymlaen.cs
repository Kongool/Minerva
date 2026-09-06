// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A32Llymlaen;

sealed class WindRose(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WindRose, 12f);
sealed class SeafoamSpiral(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SeafoamSpiral, new AOEShapeDonut(6f, 70f));
sealed class DeepDiveNormal(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DeepDiveNormal, 6f, 8);
sealed class Stormwhorl(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Stormwhorl, 6f);
sealed class Stormwinds(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Stormwinds, 6f);
sealed class Maelstrom(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Maelstrom, 6f);
sealed class Godsbane(ModuleBase module) : Components.CastCounter(module, (uint)AID.GodsbaneAOE);
sealed class DeepDiveHardWater(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.DeepDiveHardWater, 6f);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 962u, CFCID = 962u, NameID = 11299u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus, LTS (ported from BMR)")]
public sealed class A32Llymlaen(WorldState ws, Actor primary) : ModuleBase(ws, primary, DefaultCenter, DefaultBounds)
{
    public static readonly WPos DefaultCenter = new(default, -900f);
    public static readonly ArenaBoundsRect DefaultBounds = new(19f, 29f);
    private static readonly Rectangle defaultRect = new(DefaultCenter, 19f, 29f);
    public static readonly ArenaBoundsCustom EastCorridorBounds = new([defaultRect, new Rectangle(DefaultCenter + new WDir(39f, default), 40f, 10f)], ScaleFactor: 1.5f);
    public static readonly ArenaBoundsCustom WestCorridorBounds = new([defaultRect, new Rectangle(DefaultCenter + new WDir(-39f, default), 40f, 10f)], ScaleFactor: 1.5f);
}
