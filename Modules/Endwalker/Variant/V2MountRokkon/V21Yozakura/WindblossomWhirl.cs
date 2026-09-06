// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V21Yozakura;

sealed class WindblossomWhirl(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(5f, 60f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.WindblossomWhirlVisual)
        {
            _aoe = [new(donut, Center, default, Module.CastFinishAt(spell, 6.3d))];
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.WindblossomWhirl1 or (uint)AID.WindblossomWhirl2)
        {
            if (++NumCasts == 5)
            {
                _aoe = [];
                NumCasts = 0;
            }
        }
    }
}
