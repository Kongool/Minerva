// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M10STheXtremes;

sealed class ArenaChanges(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoes = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoes;
    public override void OnMapEffect(byte index, uint state)
    {
        // 01.00020001 -> regular arena
        // 01.00200010 -> donut aoe marker
        // 01.00800040 -> donut kill zone, arena turns into 20f circle instead of square
        if (index == 0x01)
        {
            switch (state)
            {
                case 0x00020001:
                    Bounds = M10STheXtremes.ArenaBounds;
                    break;
                case 0x00200010:
                    _aoes = [new(new AOEShapeDonut(20f, 30f), Center, activation: World.FutureTime(5d))];
                    break;
                case 0x00800040:
                    _aoes = [];
                    Bounds = new ArenaBoundsCircle(20f);
                    break;
            }
        }
    }
}
