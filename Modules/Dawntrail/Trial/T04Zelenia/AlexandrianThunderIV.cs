// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T04Zelenia;

sealed class AlexandrianThunderIV(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(8f);
    private static readonly AOEShapeDonut donut = new(8f, 24f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoes.Count != 0 ? CollectionsMarshal.AsSpan(_aoes)[..1] : [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.AlexandrianThunderIVCircle1 or (uint)AID.AlexandrianThunderIVCircle2 => circle,
            (uint)AID.AlexandrianThunderIVDonut1 or (uint)AID.AlexandrianThunderIVDonut2 => donut,
            _ => null
        };
        if (shape != null)
        {
            _aoes.Add(new(shape, spell.LocXZ, default, Module.CastFinishAt(spell)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0)
            switch (spell.Action.ID)
            {
                case (uint)AID.AlexandrianThunderIVCircle1:
                case (uint)AID.AlexandrianThunderIVCircle2:
                case (uint)AID.AlexandrianThunderIVDonut1:
                case (uint)AID.AlexandrianThunderIVDonut2:
                    _aoes.RemoveAt(0);
                    break;
            }
    }
}
