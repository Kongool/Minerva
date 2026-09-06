// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN6Queen;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    public readonly AOEShapeDonut donut = new(25f, 43f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.EmpyreanIniquity && Bounds.Radius > 26f)
        {
            var center = Center;
            _aoe = [new(donut, center, default, Module.CastFinishAt(spell, 4.8d), shapeDistance: donut.Distance(center, default))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x19)
        {
            if (state is 0x00020001u or 0x00400001u)
            {
                Bounds = Queen.GetDefaultArena();
                _aoe = [];
            }
            else if (state == 0x00200010u)
            {
                Bounds = new ArenaBoundsSquare(25f);
                _aoe = [];
            }
        }
    }
}
