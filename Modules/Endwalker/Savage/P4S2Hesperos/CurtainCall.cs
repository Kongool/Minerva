// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P4S2Hesperos;

// state related to curtain call mechanic
class CurtainCall(ModuleBase module) : ModuleComponent(module)
{
    private readonly int[] _playerOrder = new int[8];
    private List<Actor>? _playersInBreakOrder;
    private int _numCasts;

    public override void Update()
    {
        _playersInBreakOrder ??= [.. Raid.WithSlot(true, true, true).WhereSlot(i => _playerOrder[i] != 0).OrderBy(ip => _playerOrder[ip.Item1]).Actors()];
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_playerOrder[slot] > _numCasts)
        {
            var relOrder = _playerOrder[slot] - _numCasts;
            hints.Add($"Tether break order: {relOrder}", relOrder == 1);
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_playersInBreakOrder != null)
            hints.Add($"Order: {string.Join(" -> ", _playersInBreakOrder.Skip(_numCasts).Select(OrderTextForPlayer))}");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        // draw other players
        foreach ((var slot, var player) in Raid.WithSlot(false, true, true).Exclude(pc))
            Arena.Actor(player, _playerOrder[slot] == _numCasts + 1 ? Colors.Danger : Colors.PlayerGeneric);

        // tether
        var tetherTarget = World.Actors.Find(pc.Tether.Target);
        if (tetherTarget != null)
            Arena.AddLine(pc.Position, tetherTarget.Position, pc.Tether.ID == (uint)TetherID.WreathOfThorns ? Colors.Danger : Colors.Safe);
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.Thornpricked)
        {
            var slot = Raid.FindSlot(actor.InstanceID);
            if (slot >= 0)
            {
                _playerOrder[slot] = 2 * (int)((status.ExpireAt - World.CurrentTime).TotalSeconds / 10); // 2/4/6/8
                var ddFirst = Service.Config.Get<P4S2Config>().CurtainCallDDFirst;
                if (ddFirst != actor.Role is Role.Tank or Role.Healer)
                    --_playerOrder[slot];
                _playersInBreakOrder = null;
            }
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.Thornpricked)
            ++_numCasts;
    }

    private string OrderTextForPlayer(Actor player)
    {
        //return player.Name;
        var status = player.FindStatus((uint)SID.Thornpricked);
        var remaining = status != null ? (status.Value.ExpireAt - World.CurrentTime).TotalSeconds : 0;
        return $"{player.Name} ({remaining:f1}s)";
    }
}
