// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UCOB;

class P5Teraflare(ModuleBase module) : Components.CastCounter(module, (uint)AID.Teraflare)
{
    public bool DownForTheCountAssigned;

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.DownForTheCount)
            DownForTheCountAssigned = true;
    }
}

class P5FlamesOfRebirth(ModuleBase module) : Components.CastCounter(module, (uint)AID.FlamesOfRebirth);
