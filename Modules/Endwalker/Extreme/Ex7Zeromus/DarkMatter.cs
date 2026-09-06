// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex7Zeromus;

class DarkMatter(ModuleBase module) : Components.GenericBaitAway(module, centerAtTarget: true)
{
    private readonly List<int> _remainingCasts = [];

    private static readonly AOEShapeCircle _shape = new(8);

    public int RemainingCasts
    {
        get
        {
            if (_remainingCasts.Count > 0)
            {
                var minValue = _remainingCasts[0];
                var count = _remainingCasts.Count;
                for (var i = 0; i < count; ++i)
                {
                    var value = _remainingCasts[i];
                    if (value < minValue)
                        minValue = value;
                }
                return minValue;
            }
            else
            {
                return 0;
            }
        }
    }

    public override void Update()
    {
        for (var i = CurrentBaits.Count - 1; i >= 0; --i)
        {
            var bait = CurrentBaits[i].Target;
            if (bait.IsDestroyed || bait.IsDead)
            {
                CurrentBaits.RemoveAt(i);
                _remainingCasts.RemoveAt(i);
            }
        }
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.DarkMatter)
        {
            CurrentBaits.Add(new(Module.PrimaryActor, actor, _shape));
            _remainingCasts.Add(3);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.DarkMatterAOE)
        {
            ++NumCasts;
            var count = CurrentBaits.Count;
            for (var i = 0; i < count; ++i)
            {
                if (CurrentBaits[i].Target.InstanceID == spell.MainTargetID)
                {
                    --_remainingCasts[i];
                    return;
                }
            }
        }
    }
}

class ForkedLightningDarkBeckons(ModuleBase module) : Components.UniformStackSpread(module, 6f, 5f, 4, 4)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ForkedLightning)
            AddSpread(actor, status.ExpireAt);
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        switch (iconID)
        {
            case (uint)IconID.DarkBeckonsUmbralRays:
                AddStack(actor, World.FutureTime(5.1d));
                break;
            case (uint)IconID.DarkMatter:
                foreach (ref var s in Stacks.AsSpan())
                    s.ForbiddenPlayers.Set(Raid.FindSlot(actor.InstanceID));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ForkedLightning or (uint)AID.DarkBeckons)
        {
            Spreads.Clear();
            Stacks.Clear();
        }
    }
}
