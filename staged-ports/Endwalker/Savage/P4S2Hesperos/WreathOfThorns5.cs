// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P4S2Hesperos;

// state related to act 5 (finale) wreath of thorns
class WreathOfThorns5(ModuleBase module) : ModuleComponent(module)
{
    private readonly List<ulong> _playersOrder = [];
    private readonly List<Actor> _towersOrder = [];
    private int _castsDone;

    private const float _impulseAOERadius = 5;

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var order = _playersOrder.IndexOf(actor.InstanceID);
        if (order >= 0)
        {
            hints.Add($"Order: {order + 1}", false);

            if (order >= _castsDone && order < _towersOrder.Count)
            {
                hints.Add("Soak tower!", !actor.Position.InCircle(_towersOrder[order].Position, P4S2.WreathTowerRadius));
            }
        }

        if (_playersOrder.Count < 8)
        {
            hints.Add("Spread!", Raid.WithoutSlot(false, true, true).InRadiusExcluding(actor, _impulseAOERadius).Any());
        }
    }

    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"Order: {string.Join(" -> ", _playersOrder.Skip(_castsDone).Select(id => World.Actors.Find(id)?.Name ?? "???"))}");
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var order = _playersOrder.IndexOf(pc.InstanceID);
        if (order >= _castsDone && order < _towersOrder.Count)
            Arena.ZoneCircleOutline(_towersOrder[order].Position, P4S2.WreathTowerRadius, Colors.Safe);

        var pcTetherTarget = World.Actors.Find(pc.Tether.Target);
        if (pcTetherTarget != null)
        {
            Arena.AddLine(pc.Position, pcTetherTarget.Position, pc.Tether.ID == (uint)TetherID.WreathOfThorns ? Colors.Danger : Colors.Safe);
        }

        if (_playersOrder.Count < 8)
        {
            Arena.ZoneCircleOutline(pc.Position, _impulseAOERadius, Colors.Danger);
            foreach (var player in Raid.WithoutSlot(false, true, true).Exclude(pc))
                Arena.Actor(player, player.Position.InCircle(pc.Position, _impulseAOERadius) ? Colors.PlayerInteresting : Colors.PlayerGeneric);
        }
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID == (uint)OID.Helper)
            _towersOrder.Add(source);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch ((AID)spell.Action.ID)
        {
            case AID.FleetingImpulseAOE:
                _playersOrder.Add(spell.MainTargetID);
                break;
            case AID.AkanthaiExplodeTower:
                ++_castsDone;
                break;
        }
    }
}
