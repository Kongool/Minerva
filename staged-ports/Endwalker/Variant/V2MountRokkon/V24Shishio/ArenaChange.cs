// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V24Shishio;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(20f, 28f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.StormcloudSummons)
        {
            _aoe = [new(donut, Center, default, Module.CastFinishAt(spell, 0.7d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x64)
        {
            if (state == 0x00020001u)
            {
                Bounds = V24Shishio.CircleBounds;
                Center = V24Shishio.CircleBounds.Center;
                _aoe = [];
            }
            else if (state == 0x00080004u)
            {
                Bounds = new ArenaBoundsSquare(20f);
                Center = V24Shishio.ArenaCenter;
            }
        }
    }
}
