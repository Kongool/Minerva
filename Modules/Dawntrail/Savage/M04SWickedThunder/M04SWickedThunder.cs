// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M04SWickedThunder;

sealed class BewitchingFlight(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BewitchingFlightAOE, new AOEShapeRect(40, 2.5f));
sealed class WickedJolt(ModuleBase module) : Components.TankSwap(module, (uint)AID.WickedJolt, (uint)AID.WickedJolt, (uint)AID.WickedJoltSecond, default, 3.2d, new AOEShapeRect(60f, 2.5f));
sealed class Soulshock(ModuleBase module) : Components.CastCounter(module, (uint)AID.Soulshock);
sealed class Impact(ModuleBase module) : Components.CastCounter(module, (uint)AID.Impact);
sealed class Cannonbolt(ModuleBase module) : Components.CastCounter(module, (uint)AID.Cannonbolt);

sealed class CannonboltKB(ModuleBase module) : Components.GenericKnockback(module)
{
    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        return new Knockback[1] { new(Module.PrimaryActor.Position, 50f, ignoreImmunes: true) };
    }
}

sealed class CrossTailSwitch(ModuleBase module) : Components.CastCounter(module, (uint)AID.CrossTailSwitchAOE);
sealed class CrossTailSwitchLast(ModuleBase module) : Components.CastCounter(module, (uint)AID.CrossTailSwitchLast);
sealed class WickedSpecialCenter(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WickedSpecialCenterAOE, new AOEShapeRect(40f, 10f));
sealed class WickedSpecialSides(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WickedSpecialSidesAOE, new AOEShapeRect(40f, 7.5f));

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 992u, CFCID = 992u, NameID = 13057u, PrimaryActorOID = (uint)OID.BossP1, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class M04SWickedThunder(WorldState ws, Actor primary) : ModuleBase(ws, primary, P1DefaultCenter, P1DefaultBounds)
{
    public static readonly WPos P1DefaultCenter = new(100f, 100f);
    public static readonly WPos P2Center = new(100f, 165f);
    public static readonly ArenaBoundsSquare P1DefaultBounds = new(20f);
    public static readonly ArenaBoundsRect IonClusterBounds = new(5f, 20f);
    public static readonly ArenaBoundsRect P2DefaultBounds = new(20f, 15f);
    public static readonly ArenaBoundsCustom TransitionBounds = new([new Square(P1DefaultCenter, 20f), new Rectangle(P2Center, 20f, 15f)]);
    public static readonly ArenaBoundsCustom P2CircleBounds = new([new Polygon(P2Center, 15f, 50, 3.6f.Degrees())]);
    public static readonly ArenaBoundsCustom P2TowersBounds = new([new Rectangle(new(115f, 100f), 5f, 15f), new Rectangle(new(85f, 100f), 5f, 15f)]);

    public Actor? BossP1() => PrimaryActor.IsDestroyed ? null : PrimaryActor;
    public Actor? BossP2() => _bossP2;

    private Actor? _bossP2;

    protected override void UpdateModule()
    {
        _bossP2 ??= GetActor((uint)OID.BossP2);
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actor(_bossP2);
    }
}
