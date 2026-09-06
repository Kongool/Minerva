// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB2DeadStars;

sealed class ChillingCollision(ModuleBase module) : Components.GenericKnockback(module, (uint)AID.ChillingCollision)
{
    private Knockback[] _kb = [];

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor) => _kb;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ChillingCollisionVisual1)
        {
            _kb = [new(spell.LocXZ, 21f, Module.CastFinishAt(spell, 1.2d))];
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (_kb.Length != 0)
        {
            ref var kb = ref _kb[0];
            var act = kb.Activation;
            if (!IsImmune(slot, act))
            {
                hints.AddForbiddenZone(new SDInvertedCircle(kb.Origin, 9f), act);
            }
        }
    }
}
