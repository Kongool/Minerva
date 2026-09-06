// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA2Raiden;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeDonut donut = new(30f, 35f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Thundercall)
        {
            _aoe = [new(donut, Center, default, Module.CastFinishAt(spell, 3.4d))];
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00010002u && actor.OID == (uint)OID.Electricwall)
        {
            Bounds = BA2Raiden.DefaultArena;
            _aoe = [];
        }
    }
}
