// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Trial.T08Asura;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(19f, 20f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.LowerRealm && Bounds.Radius > 19.5f)
        {
            _aoe = [new(donut, Center, default, Module.CastFinishAt(spell, 0.8d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x01 && state == 0x00020001u)
        {
            Bounds = T08Asura.DefaultArena;
            _aoe = [];
        }
    }
}
