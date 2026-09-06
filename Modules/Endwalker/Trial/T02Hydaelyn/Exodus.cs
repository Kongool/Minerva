// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Trial.T02Hydaelyn;

class Exodus(ModuleBase module) : Components.RaidwideInstant(module, (uint)AID.Exodus, 7.2d)
{
    private int _numCrystalsDestroyed;

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == (uint)OID.CrystalOfLight)
        {
            if (++_numCrystalsDestroyed == 6)
                Activation = World.FutureTime(Delay);
        }
    }
}
