// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M05SDancingGreen;

sealed class WavelengthAlphaBeta(ModuleBase module) : ModuleComponent(module)
{
    private readonly (int Order, Actor Actor, DateTime Expiration)[] expirationBySlot = new (int, Actor, DateTime)[8];
    private int numCasts;
    private DateTime firstActivation;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (numCasts == 8)
        {
            ref var player = ref expirationBySlot[slot];

            var inRisk = false;
            if (player != default)
            {
                hints.Add($"Order: {player.Order} -", false);
            }
            for (var i = 0; i < 8; ++i)
            {
                ref var exp = ref expirationBySlot[i];
                if (exp == default || slot == i)
                {
                    continue;
                }
                var remaining = Math.Max(0d, (exp.Expiration - World.CurrentTime).TotalSeconds);
                var check = remaining < 5d;
                var partner = exp.Actor;
                if (exp.Order == player.Order)
                {
                    hints.Add($"Partner: {partner.Name} in {remaining:f0}s", check);
                }
                else if (check)
                {
                    if (actor.Position.InCircle(partner.Position, 2f))
                    {
                        inRisk = true;
                    }
                }
            }
            if (inRisk)
            {
                hints.Add($"GTFO from incorrect stacks!");
            }
        }
    }

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
    {
        if (numCasts == 8)
        {
            ref var pcOrder = ref expirationBySlot[pcSlot];
            ref var playerOrder = ref expirationBySlot[playerSlot];
            if (pcOrder.Order != default && pcOrder.Order == playerOrder.Order)
            {
                return PlayerPriority.Critical;
            }
            else if (playerOrder.Order != default)
            {
                return PlayerPriority.Danger;
            }
        }
        return PlayerPriority.Irrelevant;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (numCasts == 8)
        {
            var player = expirationBySlot[pcSlot].Order;
            for (var i = 0; i < 8; ++i)
            {
                ref var exp = ref expirationBySlot[i];
                if (exp == default || pcSlot == i)
                {
                    continue;
                }
                var remaining = Math.Max(0d, (exp.Expiration - World.CurrentTime).TotalSeconds) < 5d;
                if (!remaining)
                {
                    continue;
                }
                Arena.ZoneCircleOutline(exp.Actor.Position, 2f, exp.Order == player ? Colors.Safe : default);
            }
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.WavelengthAlpha or (uint)SID.WavelengthBeta)
        {
            var slot = World.Party.FindSlot(actor.InstanceID);
            if (firstActivation == default)
            {
                firstActivation = World.FutureTime(27.5d);
            }
            var order = (status.ExpireAt - firstActivation).TotalSeconds switch
            {
                < 4d => 1,
                < 9d => 2,
                < 14d => 3,
                _ => 4
            };
            expirationBySlot[slot] = (order, actor, status.ExpireAt);
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID is (uint)SID.WavelengthAlpha or (uint)SID.WavelengthBeta)
        {
            expirationBySlot[World.Party.FindSlot(actor.InstanceID)] = default;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.GetDownBait)
        {
            ++numCasts;
        }
    }
}
