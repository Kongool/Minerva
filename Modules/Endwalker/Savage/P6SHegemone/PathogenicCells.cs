// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P6SHegemone;

// TODO: improve...
class PathogenicCells(ModuleBase module) : Components.CastCounter(module, (uint)AID.PathogenicCellsAOE)
{
    private readonly int[] _order = new int[PartyState.MaxPartySize];

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (_order[slot] != 0)
            hints.Add($"Order: {_order[slot]}", false);
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID is >= (uint)IconID.Pathogenic1 and <= (uint)IconID.Pathogenic8)
        {
            var slot = Raid.FindSlot(actor.InstanceID);
            if (slot >= 0)
                _order[slot] = (int)iconID - (int)IconID.Pathogenic1 + 1;
        }
    }
}
