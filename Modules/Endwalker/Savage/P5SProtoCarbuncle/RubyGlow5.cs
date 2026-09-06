// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P5SProtoCarbuncle;

class RubyGlow5(ModuleBase module) : RubyGlowCommon(module)
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        // TODO: correct explosion time
        var stones = MagicStones;
        var poison = ActivePoisonAOEs();
        var countS = stones.Count;
        var len = poison.Length;
        var aoes = new AOEInstance[countS + len];
        var index = 0;
        for (var i = 0; i < countS; ++i)
        {
            aoes[index++] = new(ShapeQuadrant, QuadrantCenter(QuadrantForPosition(stones[i].Position)));
        }
        for (var i = 0; i < len; ++i)
        {
            aoes[index++] = poison[i];
        }
        return aoes;
    }
}
