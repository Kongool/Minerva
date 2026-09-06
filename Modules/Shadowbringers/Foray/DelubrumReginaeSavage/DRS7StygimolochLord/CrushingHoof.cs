// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS7StygimolochLord;

sealed class CrushingHoof(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.CrushingHoofAOE)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeCircle circle = new(25f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.CrushingHoof)
        {
            _aoe = [new(circle, spell.LocXZ, default, Module.CastFinishAt(spell, 1d))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == WatchedAction)
        {
            _aoe = [];
        }
    }
}
