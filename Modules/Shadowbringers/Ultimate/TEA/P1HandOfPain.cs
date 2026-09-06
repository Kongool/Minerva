// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P1HandOfPain(ModuleBase module) : Components.CastCounter(module, (uint)AID.HandOfPain)
{
    private readonly TEA bossmod = (TEA)module;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (bossmod.LiquidHand2 is Actor hand)
        {
            var primary = Module.PrimaryActor;
            var diff = (int)(hand.HPMP.CurHP - primary.HPMP.CurHP) * 100.0f / primary.HPMP.MaxHP;
            hints.Add($"Hand HP: {(diff > 0f ? "+" : "")}{diff:f1}%");
        }
    }
}
