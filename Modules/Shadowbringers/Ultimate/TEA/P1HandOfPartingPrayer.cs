// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

// TODO: determine when mechanic is selected; determine threshold
[SkipLocalsInit]
sealed class P1HandOfPartingPrayer(ModuleBase module) : ModuleComponent(module)
{
    private readonly TEA bossmod = (TEA)module;
    public bool Resolved;

    public override void AddGlobalHints(GlobalHints hints)
    {
        var hint = (bossmod.LiquidHand2?.ModelState.ModelState ?? default) switch
        {
            19 => "Split boss & hand",
            20 => "Stack boss & hand",
            _ => ""
        };
        if (hint.Length > 0)
            hints.Add(hint);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.HandOfParting or (uint)AID.HandOfPrayer)
        {
            Resolved = true;
        }
    }
}
