// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex7Suzaku;

sealed class Rout(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Rout, new AOEShapeRect(55f, 3f));
sealed class FleetingSummer(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.FleetingSummer, new AOEShapeCone(40f, 45f.Degrees()));
sealed class WellOfFlame(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WellOfFlame, new AOEShapeRect(41f, 10f));
sealed class ScathingNet(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.Stackmarker, (uint)AID.ScathingNet, 6f, 5.1d, 8, 8);
sealed class PhantomFlurryTB(ModuleBase module) : Components.TankSwap(module, (uint)AID.PhantomFlurryVisual, (uint)AID.PhantomFlurryTB, (uint)AID.AutoAttack2, default, 3.5d);
sealed class PhantomFlurryAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PhantomFlurryAOE, new AOEShapeCone(41f, 90f.Degrees()));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 597u, CFCID = 597u, NameID = 7702u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus), Kismet (ported from BMR)")]
public sealed class Ex7Suzaku(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, Phase1Bounds)
{
    public static readonly WPos ArenaCenter = new(100f, 100f);
    public static readonly ArenaBoundsCustom Phase1Bounds = new([new Polygon(ArenaCenter, 19.5f, 80)]);
    public static readonly ArenaBoundsCustom Phase2Bounds = new([new DonutV(ArenaCenter, 3.5f, 20f, 80)]);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.ScarletLady), Colors.Vulnerable);
        Arena.Actors(Enemies((uint)OID.ScarletPlume));
    }
}
