// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A10Lions;

class SlashAndBurn(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCircle _shapeOut = new(14f);
    private static readonly AOEShapeDonut _shapeIn = new(6f, 30f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoes.Count != 0 ? CollectionsMarshal.AsSpan(_aoes)[..1] : [];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        AOEShape? shape = spell.Action.ID switch
        {
            (uint)AID.SlashAndBurnOutFirst or (uint)AID.SlashAndBurnOutSecond or (uint)AID.TrialByFire => _shapeOut,
            (uint)AID.SlashAndBurnInFirst or (uint)AID.SlashAndBurnInSecond or (uint)AID.SpinningSlash => _shapeIn,
            _ => null
        };
        if (shape != null)
        {
            _aoes.Add(new(shape, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
            SortHelpers.SortAOEByActivation(_aoes);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0)
            switch (spell.Action.ID)
            {
                case (uint)AID.SlashAndBurnOutFirst:
                case (uint)AID.SlashAndBurnOutSecond:
                case (uint)AID.SlashAndBurnInFirst:
                case (uint)AID.SlashAndBurnInSecond:
                case (uint)AID.TrialByFire:
                case (uint)AID.SpinningSlash:
                    _aoes.RemoveAt(0);
                    break;
            }
    }
}
