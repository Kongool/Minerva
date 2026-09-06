// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Extreme.Ex7Suzaku;

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(3.5f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ScarletFever)
        {
            _aoe = [new(circle, Center, default, Module.CastFinishAt(spell, 7d))];
        }
    }

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00040008u && actor.OID == (uint)OID.RapturousEchoPlatform)
        {
            Bounds = Ex7Suzaku.Phase2Bounds;
            _aoe = [];
        }
    }
}
