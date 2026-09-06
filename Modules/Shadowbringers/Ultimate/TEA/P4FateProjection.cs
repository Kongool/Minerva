// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

// note: fate projection tethers appear before clone actors are spawned, so we're storing id's rather than actors
[SkipLocalsInit]
sealed class P4FateProjection(ModuleBase module) : ModuleComponent(module)
{
    public ulong[] Projections = new ulong[PartyState.MaxPartySize];

    public int ProjectionOwner(ulong proj) => Array.IndexOf(Projections, proj);

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID == (uint)TetherID.FateProjection)
        {
            var slot = Raid.FindSlot(source.InstanceID);
            if (slot >= 0)
            {
                Projections[slot] = tether.Target;
            }
        }
    }
}
