// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S2;

class EgoDeath(ModuleBase module) : ModuleComponent(module)
{
    public BitMask InEventMask;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.InEvent)
            InEventMask.Set(Raid.FindSlot(actor.InstanceID));
    }

    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if ((SID)status.ID == SID.InEvent)
            InEventMask.Clear(Raid.FindSlot(actor.InstanceID));
    }
}
