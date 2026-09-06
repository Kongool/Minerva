// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class P2InfernalFetters(ModuleBase module) : ModuleComponent(module)
{
    public BitMask Fetters;
    private int _fettersStrength;

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor)
    {
        return Fetters[playerSlot] ? PlayerPriority.Normal : PlayerPriority.Irrelevant;
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (Fetters.NumSetBits() > 1)
        {
            var from = Raid[Fetters.LowestSetBit()];
            var to = Raid[Fetters.HighestSetBit()];
            if (from != null && to != null)
                Arena.AddLine(from.Position, to.Position, _fettersStrength > 1 ? 0 : Colors.Safe);
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.InfernalFetters)
        {
            Fetters[Raid.FindSlot(actor.InstanceID)] = true;
            _fettersStrength = status.Extra;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.InfernalFetters)
        {
            Fetters[Raid.FindSlot(actor.InstanceID)] = false;
        }
    }
}
