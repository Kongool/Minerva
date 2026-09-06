// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex4Barbariccia;

class BrutalRush(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BrutalGust, new AOEShapeRect(40f, 2f))
{
    private BitMask _pendingRushes;
    public bool HavePendingRushes => _pendingRushes.Any();

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.BrutalRush)
            _pendingRushes[Raid.FindSlot(source.InstanceID)] = true;
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.BrutalRush)
            _pendingRushes[Raid.FindSlot(source.InstanceID)] = false;
    }
}
