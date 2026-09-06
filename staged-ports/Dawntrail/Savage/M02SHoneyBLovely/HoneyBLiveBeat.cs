// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M02SHoneyBLovely;

sealed class HoneyBLiveBeat1(ModuleBase module) : Components.CastCounter(module, (uint)AID.HoneyBLiveBeat1AOE);
sealed class HoneyBLiveBeat2(ModuleBase module) : Components.CastCounter(module, (uint)AID.HoneyBLiveBeat2AOE);
sealed class HoneyBLiveBeat3(ModuleBase module) : Components.CastCounter(module, (uint)AID.HoneyBLiveBeat3AOE);

sealed class HoneyBLiveHearts(ModuleBase module) : ModuleComponent(module)
{
    public int[] Hearts = new int[PartyState.MaxPartySize];

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        var hearts = NumHearts(status.ID);
        if (hearts >= 0 && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
            Hearts[slot] = hearts;
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        var hearts = NumHearts(status.ID);
        if (hearts >= 0 && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0 && Hearts[slot] == hearts)
            Hearts[slot] = 0;
    }

    private static int NumHearts(uint sid) => sid switch
    {
        (uint)SID.Hearts0 => 0,
        (uint)SID.Hearts1 => 1,
        (uint)SID.Hearts2 => 2,
        (uint)SID.Hearts3 => 3,
        (uint)SID.Hearts4 => 4,
        _ => -1
    };
}

abstract class Fracture(ModuleBase module) : Components.CastTowers(module, (uint)AID.Fracture, 4f)
{
    protected abstract BitMask UpdateForbidden();

    public override void Update()
    {
        var forbidden = UpdateForbidden();
        foreach (ref var t in Towers.AsSpan())
            t.ForbiddenSoakers = forbidden;
    }
}

sealed class Fracture1(ModuleBase module) : Fracture(module)
{
    private readonly HoneyBLiveHearts? _hearts = module.FindComponent<HoneyBLiveHearts>();

    protected override BitMask UpdateForbidden()
    {
        var forbidden = new BitMask();
        if (_hearts != null)
            for (var i = 0; i < _hearts.Hearts.Length; ++i)
                if (_hearts.Hearts[i] == 3)
                    forbidden.Set(i);
        return forbidden;
    }
}

sealed class Fracture2(ModuleBase module) : Fracture(module)
{
    private BitMask _spreads;
    private readonly HoneyBLiveHearts? _hearts = module.FindComponent<HoneyBLiveHearts>();

    protected override BitMask UpdateForbidden()
    {
        var forbidden = _spreads;
        if (_hearts != null)
            for (var i = 0; i < _hearts.Hearts.Length; ++i)
                if (_hearts.Hearts[i] > 1)
                    forbidden.Set(i);
        return forbidden;
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        // spread targets should never take towers
        if (iconID == (uint)IconID.Heartsore)
            _spreads.Set(Raid.FindSlot(actor.InstanceID));
    }
}

sealed class Fracture3 : Fracture
{
    private BitMask _defamations;

    public Fracture3(ModuleBase module) : base(module)
    {
        var bigBurst = module.FindComponent<HoneyBLiveBeat3BigBurst>();
        if (bigBurst != null)
        {
            var order = bigBurst.NumCasts == 0 ? 1 : 2;
            _defamations = Raid.WithSlot(true, true, true).WhereSlot(i => bigBurst.Order[i] == order).Mask();
        }
    }

    protected override BitMask UpdateForbidden() => _defamations;
}

sealed class Loveseeker(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LoveseekerAOE, 10);
sealed class HeartStruck(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HeartStruck, 6);
sealed class Heartsore(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.Heartsore, (uint)AID.Heartsore, 6, 7.1f);
sealed class SweetheartsS(ModuleBase module) : Raid.M02NHoneyBLovely.Sweethearts(module, (uint)OID.Sweetheart, (uint)AID.SweetheartTouch);

abstract class Heartsick(ModuleBase module, bool roles) : Components.StackWithIcon(module, (uint)IconID.Heartsick, (uint)AID.Heartsick, 6, 7, roles ? 2 : 4, roles ? 2 : 4)
{
    private readonly HoneyBLiveHearts? _hearts = module.FindComponent<HoneyBLiveHearts>();

    public override void Update()
    {
        if (_hearts != null)
        {
            foreach (ref var stack in Stacks.AsSpan())
            {
                for (var i = 0; i < _hearts.Hearts.Length; ++i)
                {
                    stack.ForbiddenPlayers[i] = roles
                        ? (_hearts.Hearts[i] > 0 || stack.Target.Class.IsSupport() != Raid[i]?.Class.IsSupport())
                        : _hearts.Hearts[i] == 3;
                }
            }
        }
    }
}
sealed class Heartsick1(ModuleBase module) : Heartsick(module, false);
sealed class Heartsick2(ModuleBase module) : Heartsick(module, true);

sealed class HoneyBLiveBeat3BigBurst(ModuleBase module) : Components.UniformStackSpread(module, default, 14f)
{
    public int NumCasts;
    public int[] Order = new int[PartyState.MaxPartySize];
    public readonly DateTime[] Activation = new DateTime[2];

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Order[slot] != 0)
            hints.Add($"Order: {Order[slot]}", false);
        base.AddHints(slot, actor, hints);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.PoisonNPop)
        {
            var order = (status.ExpireAt - World.CurrentTime).TotalSeconds > 30 ? 1 : 0;
            Activation[order] = status.ExpireAt;
            var slot = Raid.FindSlot(actor.InstanceID);
            if (slot >= 0)
                Order[slot] = order + 1;
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Fracture && Spreads.Count == 0)
        {
            var order = NumCasts == 0 ? 1 : 2;
            AddSpreads(Raid.WithSlot(true, true, true).WhereSlot(i => Order[i] == order).Actors(), Activation[order - 1]);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.HoneyBLiveBeat3BigBurst)
        {
            ++NumCasts;
            Spreads.Clear();
        }
    }
}
