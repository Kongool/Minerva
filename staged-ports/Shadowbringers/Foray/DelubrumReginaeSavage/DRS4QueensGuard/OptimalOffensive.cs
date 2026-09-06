// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS4QueensGuard;

sealed class OptimalOffensiveSword(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.OptimalOffensiveSword, 2.5f);
sealed class OptimalOffensiveShield(ModuleBase module) : Components.ChargeAOEs(module, (uint)AID.OptimalOffensiveShield, 2.5f);

// note: there are two casters (as usual in bozja content for raidwides)
sealed class OptimalOffensiveShieldKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.OptimalOffensiveShieldKnockback, 10f, true, 1);

sealed class UnluckyLot(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(20f);
    private AOEInstance[] _aoe = [new(circle, module.Center, default, module.World.FutureTime(7.6d))];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.OptimalOffensiveShieldMoveSphere)
        {
            _aoe = [new(circle, caster.Position, default, World.FutureTime(8.6d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.UnluckyLot)
        {
            _aoe = [];
        }
    }
}
