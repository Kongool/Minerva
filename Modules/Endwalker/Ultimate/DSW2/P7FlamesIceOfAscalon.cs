// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

sealed class P7FlamesIceOfAscalon(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    private static readonly AOEShapeCircle _shapeOut = new(8f);
    private static readonly AOEShapeDonut _shapeIn = new(8f, 50f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.GenericMechanic && actor.OID == (uint)OID.DragonKingThordan)
        {
            _aoe = [new(status.Extra == 0x12B ? _shapeIn : _shapeOut, actor.Position, default, World.FutureTime(6.2d))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.FlamesOfAscalon or (uint)AID.IceOfAscalon)
        {
            ++NumCasts;
            _aoe = [];
        }
    }
}
