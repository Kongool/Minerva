// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex6Golbez;

class VoidMeteor(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.VoidMeteorAOE, centerAtTarget: true)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.VoidMeteor)
            CurrentBaits.Add(new(Module.PrimaryActor, actor, new AOEShapeCircle(6)));
    }
}
