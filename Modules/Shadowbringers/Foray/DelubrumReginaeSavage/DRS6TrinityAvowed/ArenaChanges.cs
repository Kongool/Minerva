// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS6TrinityAvowed;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.GloryOfBozja && Bounds.Radius > 25f)
        {
            var shape = TrinityAvowed.GetArenaChangeAOE();
            var center = Center;
            _aoe = [new(shape, center, default, Module.CastFinishAt(spell, 0.7d), shapeDistance: shape.Distance(center, default))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (state == 0x00020001u)
        {
            if (index == 0x11)
            {
                Bounds = new ArenaBoundsSquare(25f);
                _aoe = [];
            }
            else if (index == 0x12)
            {
                Bounds = new ArenaBoundsRect(5f, 25f);
                Center = new(-292f, -82f);
                _aoe = [];
            }
            else if (index == 0x13)
            {
                Bounds = new ArenaBoundsRect(5f, 25f);
                Center = new(-252f, -82f);
                _aoe = [];
            }
        }
        else if (state == 0x00080004u && index is 0x12 or 0x13)
        {
            Bounds = new ArenaBoundsSquare(25f);
            Center = new(-272f, -82f);
        }
    }
}
