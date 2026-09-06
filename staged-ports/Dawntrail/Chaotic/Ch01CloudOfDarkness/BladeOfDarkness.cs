// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Chaotic.Ch01CloudOfDarkness;

sealed class BladeOfDarkness(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeDonutSector _shapeIn = new(12f, 60f, 75f.Degrees());
    private static readonly AOEShapeCone _shapeOut = new(30f, 90f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.BladeOfDarknessLAOE or (uint)AID.BladeOfDarknessRAOE => _shapeIn,
            (uint)AID.BladeOfDarknessCAOE => _shapeOut,
            _ => null
        };
        if (shape != null)
        {
            _aoe = [new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.BladeOfDarknessLAOE or (uint)AID.BladeOfDarknessRAOE or (uint)AID.BladeOfDarknessCAOE)
        {
            _aoe = [];
            ++NumCasts;
        }
    }
}
