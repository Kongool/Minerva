// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A13Engels;

class DemolishStructureArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect square = new(5f, 5f, 5f, invertForbiddenZone: true);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DemolishStructure2 && Bounds == A13MarxEngels.StartingBounds)
        {
            _aoe = [new(square, A13MarxEngels.TransitionSpot, color: Colors.SafeFromAOE)];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x0B && state == 0x00020001u)
        {
            Center = A13MarxEngels.SecondArenaCenter;
            Bounds = A13MarxEngels.StartingBounds;
            _aoe = [];
        }
    }
}

class MarxSmash1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxSmash1, new AOEShapeRect(60, 15));
class MarxSmash2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxSmash2, new AOEShapeRect(60, 15));
class MarxSmash3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxSmash3, new AOEShapeRect(60, 15));
class MarxSmash4(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxSmash4, new AOEShapeRect(30, 30));
class MarxSmash5(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxSmash5, new AOEShapeRect(35, 30));
class MarxSmash6(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxSmash6, new AOEShapeRect(60, 10));
class MarxSmash7(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxSmash7, new AOEShapeRect(60, 10));

class MarxCrush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MarxCrush, new AOEShapeRect(15, 15));

class PrecisionGuidedMissile2(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.PrecisionGuidedMissile2, 6);
class LaserSight1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LaserSight1, new AOEShapeRect(100, 10));
class GuidedMissile2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GuidedMissile2, 6);
class IncendiaryBombing2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IncendiaryBombing2, 8);
class IncendiaryBombing1(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Spreadmarker, (uint)AID.IncendiaryBombing1, 8, 5);
class DiffuseLaser(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.DiffuseLaser);
class SurfaceMissile2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SurfaceMissile2, 6);

class GuidedMissile(ModuleBase module) : Components.StandardChasingAOEs(module, 6f, (uint)AID.GuidedMissile2, (uint)AID.GuidedMissile3, 5.5f, 1d, 4, true, (uint)IconID.GuidedMissile);

[ModuleInfo(CFCID = 700u, NameID = 9147u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (ported from BMR)")]
public class A13MarxEngels(WorldState ws, Actor primary) : ModuleBase(ws, primary, StartingArenaCenter, StartingBounds)
{
    public static readonly WPos TransitionSpot = new(900, 697);
    public static readonly WPos StartingArenaCenter = new(900, 670);
    public static readonly WPos SecondArenaCenter = new(900, 785);
    public static readonly ArenaBoundsSquare StartingBounds = new(30);
}
