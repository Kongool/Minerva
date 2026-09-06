// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex1Valigarmanda;

sealed class RuinfallTower(ModuleBase module) : Components.GenericTowers(module, (uint)AID.RuinfallTower)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            var party = Module.Raid.WithSlot(true, true, true);
            var len = party.Length;
            BitMask nontanks = default;
            for (var i = 0; i < len; ++i)
            {
                ref readonly var p = ref party[i];
                if (p.Item2.Role != Role.Tank)
                {
                    nontanks[p.Item1] = true;
                }
            }
            Towers.Add(new(spell.LocXZ, 6f, 2, 2, nontanks, Module.CastFinishAt(spell)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            Towers.Clear();
    }
}

sealed class RuinfallKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.RuinfallKnockback, 25f, kind: Kind.DirForward);
sealed class RuinfallAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RuinfallAOE, 6f);
