// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A10RhalgrEmissary;

class DestructiveStatic(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DestructiveStatic, new AOEShapeCone(50f, 90f.Degrees()));
class LightningBolt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LightningBoltAOE, 6f);
class BoltsFromTheBlue(ModuleBase module) : Components.CastCounter(module, (uint)AID.BoltsFromTheBlueAOE);
class DestructiveStrike(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.DestructiveStrike, new AOEShapeCone(13f, 60f.Degrees()), endsOnCastEvent: true, tankbuster: true);

[ModuleInfo(CFCID = 866u, NameID = 11274u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class A10RhalgrEmissary(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(73.941f, 516.059f), 24.5f * CosPI.Pi148th, 148)], [new Rectangle(new(91.918f, 498.082f), 20f, 1.25f, -45f.Degrees()),
    new Rectangle(new(74f, 541.4f), 20f, 1.25f)]);
}
