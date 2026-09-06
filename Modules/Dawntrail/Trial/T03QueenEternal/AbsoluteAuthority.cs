// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T03QueenEternal;

sealed class AbsoluteAuthorityCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AbsoluteAuthorityCircle, 8f);

sealed class AbsoluteAuthorityFlare(ModuleBase module) : Components.BaitAwayIcon(module, 12f, (uint)IconID.Flare, (uint)AID.AbsoluteAuthorityFlare, 6f)
{
    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        base.AddHints(slot, actor, hints);
        if (IsBaitTarget(actor))
        {
            hints.Add("Bait away!");
        }
    }
}

sealed class AbsoluteAuthorityDorito(ModuleBase module) : Components.GenericStackSpread(module)
{
    private readonly AbsoluteAuthorityCircle _aoe = module.FindComponent<AbsoluteAuthorityCircle>()!;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.DoritoStack && Stacks.Count == 0)
        {
            Stacks.Add(new(actor, 3f, 8, 8, activation: World.FutureTime(5.1d)));
        }
    }

    public override void Update()
    {
        if (Stacks.Count != 0)
        {
            var party = Raid.WithoutSlot(false, true, true);
            var player = Raid.Player()!;
            var pPos = player.Position;
            Actor? closest = null;

            var minDistSq = float.MaxValue;
            var len = party.Length;
            var aoes = _aoe.ActiveAOEs(default, player);
            var lenaoes = aoes.Length;
            for (var i = 0; i < len; ++i)
            {
                var p = party[i];

                if (p == player)
                    continue;
                var distSq = (p.Position - pPos).LengthSq();

                if (distSq < minDistSq)
                {
                    for (var j = 0; j < lenaoes; ++j)
                    {
                        if (aoes[j].Check(p.Position))
                        {
                            goto next;
                        }
                    }
                    minDistSq = distSq;
                    closest = p;
                }
            next:
                ;
            }

            Stacks.Ref(0).Target = closest ?? player;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.AbsoluteAuthorityDoritoStack1 or (uint)AID.AbsoluteAuthorityDoritoStack2)
            Stacks.Clear();
    }
}

sealed class AuthoritysHold(ModuleBase module) : Components.StayMove(module, 3)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.AuthoritysHold && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            PlayerStates[slot] = new(Requirement.Stay, status.ExpireAt);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.AuthoritysHold && Raid.FindSlot(actor.InstanceID) is var slot && slot >= 0)
        {
            PlayerStates[slot] = default;
        }
    }
}

sealed class AuthoritysGaze(ModuleBase module) : Components.GenericGaze(module)
{
    private DateTime _activation;
    private readonly List<Actor> _affected = [];

    public override ReadOnlySpan<Eye> ActiveEyes(int slot, Actor actor)
    {
        var count = _affected.Count;
        if (count == 0 || World.CurrentTime < _activation.AddSeconds(-10d))
        {
            return [];
        }
        var eyes = new Eye[count];
        for (var i = 0; i < count; ++i)
        {
            eyes[i] = new(_affected[i].Position, _activation);
        }
        return eyes;
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.AuthoritysGaze)
        {
            _activation = status.ExpireAt;
            _affected.Add(actor);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.AuthoritysGaze)
        {
            _affected.Remove(actor);
        }
    }
}
