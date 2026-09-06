// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex4Ifrit;

class Hellfire(ModuleBase module) : ModuleComponent(module)
{
    private DateTime _expectedRaidwide = module.StateMachine.NextTransitionWithFlag(StateMachine.StateHint.Raidwide);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        hints.AddPredictedDamage(Raid.WithSlot(false, true, true).Mask(), _expectedRaidwide);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.Hellfire)
            _expectedRaidwide = Module.CastFinishAt(spell);
    }
}
