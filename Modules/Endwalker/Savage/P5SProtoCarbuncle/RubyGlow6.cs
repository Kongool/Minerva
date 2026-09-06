// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P5SProtoCarbuncle;

class RubyGlow6(ModuleBase module) : RubyGlowRecolor(module, 9)
{
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        // TODO: correct explosion time
        var condition = CurRecolorState != RecolorState.BeforeStones && MagicStones.Count != 0;
        var poison = ActivePoisonAOEs();
        var len = poison.Length;
        var aoes = new AOEInstance[(condition ? 1 : 0) + len];
        var index = 0;
        if (condition)
            aoes[index++] = new(ShapeQuadrant, QuadrantCenter(AOEQuadrant));
        for (var i = 0; i < len; ++i)
        {
            aoes[index++] = poison[i];
        }
        return aoes;
    }
}
