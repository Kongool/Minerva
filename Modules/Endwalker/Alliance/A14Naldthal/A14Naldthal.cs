// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A14Naldthal;

class GoldenTenet(ModuleBase module) : Components.CastSharedTankbuster(module, (uint)AID.GoldenTenetAOE, 6f);
class StygianTenet(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.StygianTenetAOE, 3f, true, tankbuster: true);

class HellOfFire(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.HellOfFireFrontAOE, (uint)AID.HellOfFireBackAOE], new AOEShapeCone(60f, 90f.Degrees()));

class WaywardSoul(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WaywardSoulAOE, 18f, 3);
class SoulVessel(ModuleBase module) : Components.Adds(module, (uint)OID.SoulVesselReal);
class Twingaze(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Twingaze, new AOEShapeCone(60f, 15f.Degrees()));
class MagmaticSpell(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.MagmaticSpellAOE, 6f, 8, 24);

[ModuleInfo(CFCID = 866u, NameID = 11286u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class A14Naldthal(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(750f, -750f), 29f, 180)]);
}
