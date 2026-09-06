// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRS8Queen;

sealed class FieryIcyPortent(ModuleBase module) : Components.StayMove(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var req = spell.Action.ID switch
        {
            (uint)AID.FieryPortent => Requirement.Stay,
            (uint)AID.IcyPortent => Requirement.Move,
            _ => Requirement.None
        };
        if (req != Requirement.None)
        {
            Array.Fill(PlayerStates, new(req, Module.CastFinishAt(spell)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.FieryPortent or (uint)AID.IcyPortent)
        {
            Array.Clear(PlayerStates);
        }
    }
}
