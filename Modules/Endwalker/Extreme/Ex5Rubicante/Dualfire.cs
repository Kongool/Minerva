// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex5Rubicante;

class Dualfire(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.DualfireAOE)
{
    private static readonly AOEShapeCone _shape = new(60, 60.Degrees()); // TODO: verify angle

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.Dualfire)
            CurrentBaits.Add(new(Module.PrimaryActor, actor, _shape));
    }
}
