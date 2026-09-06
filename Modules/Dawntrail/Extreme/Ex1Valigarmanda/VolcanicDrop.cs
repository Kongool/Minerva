// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex1Valigarmanda;

sealed class VolcanicDrop(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.VolcanicDropAOE)
{
    public AOEInstance[] AOE = [];

    private readonly AOEShapeCircle circle = new(20f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => AOE;

    public override void OnMapEffect(byte index, uint state)
    {
        // state transitions:
        // 00020001 - both volcanos, appear after skyruin end
        // 00200010 - active volcano, eruption start after triscourge end
        // 00800040 - active volcano, some eruption animation
        // 02000100 - active volcano, eruption end after puddles
        if (index is 0x0E or 0x0F && state == 0x00200010u)
        {
            var pos = (Center + new WDir(index == 0x0E ? 13f : -13f, default)).Quantized();
            AOE = [new(circle, pos, default, World.FutureTime(7.8d), shapeDistance: circle.Distance(pos, default))];
        }
    }
}

sealed class VolcanicDropPuddle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.VolcanicDropPuddle, 2f);
