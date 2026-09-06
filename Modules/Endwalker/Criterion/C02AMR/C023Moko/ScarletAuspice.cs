// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C02AMR.C023Moko;

abstract class ScarletAuspice(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, 6f);
sealed class NScarletAuspice(ModuleBase module) : ScarletAuspice(module, (uint)AID.NScarletAuspice);
sealed class SScarletAuspice(ModuleBase module) : ScarletAuspice(module, (uint)AID.SScarletAuspice);

abstract class BoundlessScarletFirst(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(60f, 5f));
sealed class NBoundlessScarletFirst(ModuleBase module) : BoundlessScarletFirst(module, (uint)AID.NBoundlessScarletAOE);
sealed class SBoundlessScarletFirst(ModuleBase module) : BoundlessScarletFirst(module, (uint)AID.SBoundlessScarletAOE);

abstract class BoundlessScarletRest(ModuleBase module, uint aid) : Components.SimpleAOEs(module, aid, new AOEShapeRect(60f, 15f), 2);
sealed class NBoundlessScarletRest(ModuleBase module) : BoundlessScarletRest(module, (uint)AID.NBoundlessScarletExplosion);
sealed class SBoundlessScarletRest(ModuleBase module) : BoundlessScarletRest(module, (uint)AID.SBoundlessScarletExplosion);

sealed class InvocationOfVengeance(ModuleBase module) : Components.UniformStackSpread(module, 3, 3)
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
            case (uint)SID.VengefulFlame:
                _spreadTargets.Add(actor);
                _spreadResolve = status.ExpireAt;
                UpdateStackSpread();
                break;
            case (uint)SID.VengefulPyre:
                _stackTargets.Add(actor);
                _stackResolve = status.ExpireAt;
                UpdateStackSpread();
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.NVengefulFlame:
            case (uint)AID.SVengefulFlame:
                if (_spreadResolve != default)
                {
                    ++NumMechanics;
                    _spreadTargets.Clear();
                    _spreadResolve = default;
                    UpdateStackSpread();
                }
                break;
            case (uint)AID.NVengefulPyre:
            case (uint)AID.SVengefulPyre:
                if (_stackResolve != default)
                {
                    ++NumMechanics;
                    _stackTargets.Clear();
                    _stackResolve = default;
                    UpdateStackSpread();
                }
                break;
        }
    }

    private void UpdateStackSpread()
    {
        Spreads.Clear();
        Stacks.Clear();
        if (_stackResolve == default || _stackResolve > _spreadResolve)
            AddSpreads(_spreadTargets, _spreadResolve);
        if (_spreadResolve == default || _spreadResolve > _stackResolve)
            AddStacks(_stackTargets, _stackResolve);
    }
}
