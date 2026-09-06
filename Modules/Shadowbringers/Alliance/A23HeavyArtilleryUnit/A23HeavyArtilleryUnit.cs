// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A23HeavyArtilleryUnit;

sealed class ManeuverVoltArray(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ManeuverVoltArray);
sealed class EnergyBombardment(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.EnergyBombardment, 4f);
sealed class R010Laser(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.R010Laser, new AOEShapeRect(60f, 6f));
sealed class R030Hammer(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.R030Hammer, 18f);
sealed class ManeuverHighPoweredLaser(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeRect(60f, 4f), (uint)IconID.ManeuverHighPoweredLaser, (uint)AID.ManeuverHighPoweredLaser, 5.4d, tankbuster: true);
sealed class UnconventionalVoltage(ModuleBase module) : Components.BaitAwayIcon(module, new AOEShapeCone(60f, 15f.Degrees()), (uint)IconID.UnconventionalVoltage, (uint)AID.UnconventionalVoltage, 6.8d);

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 736u, CFCID = 736u, NameID = 9650u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class A23HeavyArtilleryUnit(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new DonutV(new(200f, -100f), 6.5f, 29.5f, 192)]);
}
