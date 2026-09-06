// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T04Zelenia;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(4f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.QueensCrusade)
        {
            _aoe = [new(circle, Center, default, Module.CastFinishAt(spell, 0.1d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index != 0x01)
        {
            return;
        }
        switch (state)
        {
            case 0x00020001u:
                _aoe = [];
                Bounds = T04Zelenia.DonutArena;
                break;
            case 0x00080004u:
                Bounds = T04Zelenia.DefaultArena;
                break;
        }
    }
}
