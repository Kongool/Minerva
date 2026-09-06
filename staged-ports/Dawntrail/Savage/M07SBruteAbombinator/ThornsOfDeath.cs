// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M07SBruteAbombinator;

sealed class ThornsOfDeath(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<(Actor Player, Actor Source)> _tethers = [];
    public BitMask TetheredPlayers;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var count = _tethers.Count;
        for (var i = 0; i < count; ++i)
        {
            var t = _tethers[i];
            Arena.AddLine(t.Source.Position, t.Player.Position);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (TetheredPlayers != default)
            hints.AddPredictedDamage(TetheredPlayers, World.FutureTime(1d));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!TetheredPlayers[slot])
            return;

        if (actor.Role != Role.Tank)
        {
            hints.Add("Stay close to tethered wall!");
        }
        else
        {
            hints.Add("Stay close to tethered wall or pass aggro to co-tank if needed!");
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID is (uint)TetherID.ThornsOfDeathNonTank or (uint)TetherID.ThornsOfDeathTank or (uint)TetherID.ThornsOfDeathTakeable)
        {
            var target = World.Actors.Find(tether.Target);
            if (target is Actor t)
            {
                _tethers.Add((t, source));
                TetheredPlayers[Raid.FindSlot(tether.Target)] = true;
            }
        }
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID is (uint)TetherID.ThornsOfDeathNonTank or (uint)TetherID.ThornsOfDeathTank or (uint)TetherID.ThornsOfDeathTakeable)
        {
            var target = World.Actors.Find(tether.Target);
            if (target is Actor t)
            {
                _tethers.Remove((t, source));
                TetheredPlayers[Raid.FindSlot(tether.Target)] = false;
            }
        }
    }
}
