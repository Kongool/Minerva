// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C021Shishio;

sealed class UnnaturalWail(ModuleBase module) : Components.UniformStackSpread(module, 6f, 6f, 2, 2)
{
    public int NumMechanics;
    private readonly List<Actor> _spreadTargets = [];
    private readonly List<Actor> _stackTargets = [];
    private DateTime _spreadResolve;
    private DateTime _stackResolve;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_spreadResolve == default || _stackResolve == default)
            return;
        var orderHint = _spreadResolve > _stackResolve ? $"Stack -> Spread" : $"Spread -> Stack";
        hints.Add($"Debuff order: {orderHint}");
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        switch (status.ID)
        {
            case (uint)SID.ScatteredWailing:
                _spreadTargets.Add(actor);
                _spreadResolve = status.ExpireAt;
                UpdateMechanic();
                break;
            case (uint)SID.IntensifiedWailing:
                _stackTargets.Add(actor);
                _stackResolve = status.ExpireAt;
                UpdateMechanic();
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.NUnnaturalAilment:
            case (uint)AID.SUnnaturalAilment:
                if (_spreadResolve != default)
                {
                    _spreadResolve = default;
                    ++NumMechanics;
                    UpdateMechanic();
                }
                break;
            case (uint)AID.NUnnaturalForce:
            case (uint)AID.SUnnaturalForce:
                if (_stackResolve != default)
                {
                    _stackResolve = default;
                    ++NumMechanics;
                    UpdateMechanic();
                }
                break;
        }
    }

    private void UpdateMechanic()
    {
        Stacks.Clear();
        Spreads.Clear();
        if (_stackResolve != default && (_spreadResolve == default || _spreadResolve > _stackResolve))
            AddStacks(_stackTargets, _stackResolve);
        if (_spreadResolve != default && (_stackResolve == default || _stackResolve > _spreadResolve))
            AddSpreads(_spreadTargets, _spreadResolve);
    }
}
