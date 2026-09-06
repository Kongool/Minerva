// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex1Valigarmanda;

sealed class FireScourgeOfFire(ModuleBase module) : Components.UniformStackSpread(module, 5f, default, 4)
{
    private readonly List<int> _remainingCasts = [];

    // unfortunately, targets can die...
    public int RemainingCasts()
    {
        var max = 0;
        for (var i = 0; i < Stacks.Count; ++i)
        {
            if (Stacks[i].Target.IsDead)
                continue;
            if (_remainingCasts[i] > max)
                max = _remainingCasts[i];
        }
        return max;
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.CalamitysInferno)
        {
            AddStack(actor, World.FutureTime(7.1d));
            _remainingCasts.Add(3);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FireScourgeOfFire)
        {
            var index = Stacks.FindIndex(s => s.Target.InstanceID == spell.MainTargetID);
            if (index >= 0)
                --_remainingCasts[index];
        }
    }
}

sealed class FireScourgeOfFireVoidzone(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 5, (uint)AID.FireScourgeOfFire, module => module.Enemies((uint)OID.ScourgeOfFireVoidzone).Where(z => z.EventState != 7), 0.9f);

sealed class FireScourgeOfIce(ModuleBase module) : Components.StayMove(module)
{
    public int NumImminent;
    public int NumActiveFreezes;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.FreezingUp)
            ++NumActiveFreezes;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.FreezingUp)
            --NumActiveFreezes;
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.CalamitysChill && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            PlayerStates[slot] = new(Requirement.Move, World.FutureTime(7));
            ++NumImminent;
        }
    }
}

sealed class IceScourgeOfFireIce(ModuleBase module) : Components.IconStackSpread(module, (uint)IconID.CalamitysInferno, (uint)IconID.CalamitysChill, (uint)AID.IceScourgeOfFire, (uint)AID.IceScourgeOfIce, 5f, 16f, 7.1f, 3, 3);
sealed class FireIceScourgeOfThunder(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.CalamitysBolt, (uint)AID.FireIceScourgeOfThunder, 5f, 7.1d);

// TODO: add hint if player and stack target has different levitate states
sealed class ThunderScourgeOfFire(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.CalamitysInferno, (uint)AID.ThunderScourgeOfFire, 5f, 7.1d, 4, 4);

// TODO: verify spread radius for ice boulders...
sealed class ThunderScourgeOfIceThunder(ModuleBase module) : Components.UniformStackSpread(module, default, 8f)
{
    public int NumCasts;
    private readonly ThunderPlatform? _platform = module.FindComponent<ThunderPlatform>();

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID is (uint)IconID.CalamitysBolt or (uint)IconID.CalamitysChill)
        {
            AddSpread(actor, World.FutureTime(7.1d));
            var slot = Raid.FindSlot(actor.InstanceID);
            if (slot >= 0 && _platform != null)
            {
                _platform.RequireHint[slot] = true;
                _platform.RequireLevitating[slot] = iconID == (uint)IconID.CalamitysChill;
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ThunderScourgeOfThunderFail or (uint)AID.ThunderScourgeOfThunder)
        {
            ++NumCasts;
            Spreads.Clear();
        }
    }
}
