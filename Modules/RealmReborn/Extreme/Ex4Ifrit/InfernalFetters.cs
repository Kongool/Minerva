// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex4Ifrit;

class InfernalFetters(ModuleBase module) : ModuleComponent(module)
{
    public BitMask Fetters;
    private int _fettersStrength;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Fetters[slot] && actor.Role != Role.Tank)
        {
            var partner = Raid.WithSlot(false, true, true).Exclude(slot).IncludedInMask(Fetters).FirstOrDefault().Item2;
            if (partner != null)
                hints.AddForbiddenZone(new SDInvertedCircle(partner.Position, 10)); // TODO: tweak range...
        }
    }

    public override PlayerPriority CalcPriority(int pcSlot, Actor pc, int playerSlot, Actor player, ref uint customColor) => Fetters[playerSlot] ? PlayerPriority.Normal : PlayerPriority.Irrelevant;

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        if (Fetters.NumSetBits() > 1)
        {
            var from = Raid[Fetters.LowestSetBit()];
            var to = Raid[Fetters.HighestSetBit()];
            if (from != null && to != null)
                Arena.AddLine(from.Position, to.Position, _fettersStrength > 1 ? Colors.Danger : Colors.Safe);
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.InfernalFetters)
        {
            Fetters.Set(Raid.FindSlot(actor.InstanceID));
            _fettersStrength = status.Extra;
        }
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.InfernalFetters)
        {
            Fetters.Clear(Raid.FindSlot(actor.InstanceID));
            _fettersStrength = 0;
        }
    }
}
