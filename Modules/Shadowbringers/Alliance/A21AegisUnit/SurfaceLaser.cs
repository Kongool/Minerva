// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A21AegisUnit;

sealed class SurfaceLaserSpread(ModuleBase module) : Components.GenericStackSpread(module, true)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.SurfaceLaser)
        {
            Spreads.Add(new(actor, 4f, World.FutureTime(5.1d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SurfaceLaserLock)
        {
            Spreads.Clear();
        }
    }
}

sealed class SurfaceLaserAOE(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(4f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var id = spell.Action.ID;
        if (id == (uint)AID.SurfaceLaserLock)
        {
            _aoes.Add(new(circle, caster.Position.Quantized()));
        }
        else if (id == (uint)AID.SurfaceLaser && ++NumCasts == 10 * _aoes.Count)
        {
            _aoes.Clear();
            NumCasts = 0;
        }
    }
}
