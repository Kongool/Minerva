// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P3SPhoinix;

class HeatOfCondemnation(ModuleBase module) : Components.TankbusterTether(module, (uint)AID.HeatOfCondemnationAOE, (uint)TetherID.HeatOfCondemnation, 6);
class TrailOfCondemnationAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TrailOfCondemnationAOE, new AOEShapeRect(40, 7.5f));
class SearingBreeze(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SearingBreezeAOE, 6);

abstract class Cinderwing(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeCone(60, 90.Degrees()));
class LeftCinderwing(ModuleBase module) : Cinderwing(module, (uint)AID.LeftCinderwing);
class RightCinderwing(ModuleBase module) : Cinderwing(module, (uint)AID.RightCinderwing);

class DevouringBrand(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCross cross = new(40, 5);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DevouringBrandAOE)
        {
            _aoe = [new(cross, spell.LocXZ, default, Module.CastFinishAt(spell, 2.2d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x00 && state == 0x00080004u)
        {
            _aoe = [];
        }
    }
}

class SunBirdLarge(ModuleBase module) : Components.Adds(module, (uint)OID.SunbirdLarge)
{
    public int FinishedTethers;
    public override void Update()
    {
        var comp = Module.FindComponent<BirdTether>();
        if (comp != null)
            FinishedTethers = comp.NumFinishedChains;
    }
}

class SunBirdSmall(ModuleBase module) : Components.Adds(module, (uint)OID.SunbirdSmall);
class DarkenedFireAdd(ModuleBase module) : Components.Adds(module, (uint)OID.DarkenedFire);

[ModuleInfo(CFCID = 807u, NameID = 10720u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class P3S(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(100f, 100f), new ArenaBoundsCircle(20f));
