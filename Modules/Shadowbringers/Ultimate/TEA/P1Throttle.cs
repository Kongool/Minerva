// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P1Throttle(ModuleBase module) : Components.CleansableDebuff(module, (uint)SID.Throttle, "Throttle", "throttled")
{
    public bool Applied;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        base.OnStatusGain(actor, ref status);
        if (status.ID == (uint)SID.Throttle)
        {
            Applied = true;
        }
    }
}
