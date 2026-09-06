// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P10SPandaemonium;

class SoulGrasp(ModuleBase module) : Components.GenericSharedTankbuster(module, (uint)AID.SoulGraspAOE, 4f)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.SoulGrasp)
        {
            Source = Module.PrimaryActor;
            Target = actor;
            Activation = World.FutureTime(5.8d);
        }
    }
}
