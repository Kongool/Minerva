// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB1DemonTablet;

sealed class LacunateStream(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeRect rect = new(34f, 15f, 3f);
    private int roateCounter;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var id = spell.Action.ID;
        if (id is (uint)AID.RotateLeft or (uint)AID.RotateRight)
        {
            if (++roateCounter == 2)
            {
                _aoe = [new(rect, spell.LocXZ, spell.Rotation + (id == (uint)AID.RotateLeft ? 1f : -1f) * 90f.Degrees(), Module.CastFinishAt(spell, 4.2d))];
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.LacunateStreamRepeat)
        {
            ++NumCasts;
        }
    }
}
