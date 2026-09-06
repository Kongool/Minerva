// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A24Menphina;

sealed class WinterHalo(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];

    private static readonly AOEShapeDonut _shape = new(10f, 60f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WinterHaloShortAOE or (uint)AID.WinterHaloLongMountedAOE or (uint)AID.WinterHaloLongDismountedAOE)
        {
            _aoe = [new(_shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WinterHaloShortAOE or (uint)AID.WinterHaloLongMountedAOE or (uint)AID.WinterHaloLongDismountedAOE)
        {
            ++NumCasts;
            _aoe = [];
        }
    }
}
