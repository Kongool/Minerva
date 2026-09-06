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
sealed class P4OpticalSight(ModuleBase module) : Components.UniformStackSpread(module, 6f, 6f, 4, 4)
{
    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        switch (iconID)
        {
            case (uint)IconID.OpticalSightSpread:
                AddSpread(actor, World.FutureTime(5.1d));
                break;
            case (uint)IconID.OpticalSightStack:
                AddStack(actor, World.FutureTime(5.1d));
                break;
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.IndividualReprobation:
                Spreads.Clear();
                break;
            case (uint)AID.CollectiveReprobation:
                Stacks.Clear();
                break;
        }
    }
}
