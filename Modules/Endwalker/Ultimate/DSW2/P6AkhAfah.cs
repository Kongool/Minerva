// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

sealed class P6HPCheck(ModuleBase module) : ModuleComponent(module)
{
    private readonly DSW2 bossmodule = (DSW2)module;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (bossmodule._NidhoggP6 is Actor nidhogg && bossmodule._HraesvelgrP6 is Actor hraesvelgr)
        {
            var diff = (int)(nidhogg.HPMP.CurHP - hraesvelgr.HPMP.CurHP) * 100.0f / nidhogg.HPMP.MaxHP;
            hints.Add($"Nidhogg HP: {(diff > 0 ? "+" : "")}{diff:f1}%");
        }
    }
}

sealed class P6AkhAfah(ModuleBase module) : Components.UniformStackSpread(module, 4f, default, 4)
{
    public bool Done;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.AkhAfahN)
            AddStacks(Raid.WithoutSlot(true, true, true).Where(p => p.Role == Role.Healer));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.AkhAfahHAOE or (uint)AID.AkhAfahNAOE)
        {
            Stacks.Clear();
            Done = true;
        }
    }
}
