// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A22SuperiorFlightUnits;

sealed class HighOrderExplosiveBlastCross(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCross cross = new(20f, 2.5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HighOrderExplosiveBlastCircle)
        {
            var pos = spell.LocXZ;
            var activation = Module.CastFinishAt(spell, 2.1d);
            AddAOE(Angle.AnglesIntercardinals[1]);
            AddAOE(Angle.AnglesCardinals[1]);
            void AddAOE(Angle angle) => _aoes.Add(new(cross, pos, angle, activation));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.HighOrderExplosiveBlastCross)
        {
            _aoes.RemoveAt(0);
        }
    }
}
