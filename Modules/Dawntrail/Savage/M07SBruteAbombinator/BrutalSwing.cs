// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M07SBruteAbombinator;

sealed class BrutalSwing(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCone cone = new(25f, 90f.Degrees());
    private static readonly AOEShapeCircle circle = new(12f);
    private static readonly AOEShapeDonut donut = new(9f, 60f);
    private static readonly AOEShapeDonutSector donutSector = new(22f, 88f, 90f.Degrees());
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.BrutishSwingCircle => circle,
            (uint)AID.BrutishSwingCone1 or (uint)AID.BrutishSwingCone2 => cone,
            (uint)AID.BrutishSwingDonut => donut,
            (uint)AID.BrutishSwingDonutSegment1 or (uint)AID.BrutishSwingDonutSegment2 => donutSector,
            _ => null
        };
        if (shape != null)
        {
            _aoe = [new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.BrutishSwingCircle:
            case (uint)AID.BrutishSwingCone1:
            case (uint)AID.BrutishSwingCone2:
            case (uint)AID.BrutishSwingDonut:
            case (uint)AID.BrutishSwingDonutSegment1:
            case (uint)AID.BrutishSwingDonutSegment2:
                _aoe = [];
                ++NumCasts;
                break;
        }
    }
}
