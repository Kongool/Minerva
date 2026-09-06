// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M04NWickedThunder;

sealed class WickedHypercannon(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect rect = new(40f, 10f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WickedHypercannonVisual2 or (uint)AID.WickedHypercannonVisual3)
        {
            if (Center == ArenaChanges.EastremovedCenter)
            {
                AddAOE(new(85f, 80f));
            }
            else if (Center == ArenaChanges.WestRemovedCenter)
            {
                AddAOE(new(115f, 80f));
            }
            void AddAOE(WPos pos) => _aoe = [new(rect, pos, default, Module.CastFinishAt(spell, 0.6d), Colors.SafeFromAOE, false)];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.WickedHypercannon)
        {
            if (++NumCasts == 10)
            {
                _aoe = [];
                NumCasts = 0;
            }
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_aoe.Length != 0)
        {
            ref var aoe = ref _aoe[0];
            hints.AddPredictedDamage(Raid.WithSlot(false, true, true).Mask(), aoe.Activation);
        }
    }
}
