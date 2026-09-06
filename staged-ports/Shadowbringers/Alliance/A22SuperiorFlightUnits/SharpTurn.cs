// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A22SuperiorFlightUnits;

sealed class SharpTurn(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(30f, 130f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var offset = spell.Action.ID switch
        {
            (uint)AID.SharpTurnAlpha1 or (uint)AID.SharpTurnBeta1 or (uint)AID.SharpTurnChi1 => -1f,
            (uint)AID.SharpTurnAlpha2 or (uint)AID.SharpTurnBeta2 or (uint)AID.SharpTurnChi2 => 1f,
            _ => default
        };
        if (offset != default)
        {
            _aoes.Add(new(rect, spell.LocXZ, spell.Rotation + offset * 90f.Degrees(), Module.CastFinishAt(spell, 1.4d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.SharpTurn1 or (uint)AID.SharpTurn2)
        {
            _aoes.Clear();
        }
    }
}
