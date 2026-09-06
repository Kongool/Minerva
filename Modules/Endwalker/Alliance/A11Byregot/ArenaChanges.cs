// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A11Byregot;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module) // arena changes excluding hammer phase
{
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x00)
        {
            if (state == 0x00020001u)
            {
                Bounds = new ArenaBoundsSquare(24f);
                _aoe = [];
            }
            else if (state == 0x00080004u)
            {
                Bounds = new ArenaBoundsSquare(24.5f);
            }
        }
        else if (index == 0x4F && state == 0x00080004u)
        {
            Bounds = new ArenaBoundsSquare(24.5f);
            Center = new(0f, 700f);
            AddAOE(World.FutureTime(10.6d));
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.OrdealOfThunder && Bounds.Radius > 24f)
        {
            AddAOE(Module.CastFinishAt(spell, 0.9d));
        }
    }

    private void AddAOE(DateTime act)
    {
        var center = Center;
        var shape = new AOEShapeCustom(center, [new Square(center, 24.5f)], [new Square(center, 24f)]);
        _aoe = [new(shape, center, default, act, shapeDistance: shape.Distance(center, default))];
    }
}
