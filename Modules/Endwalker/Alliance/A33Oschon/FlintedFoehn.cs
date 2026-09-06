// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A33Oschon;

class P1FlintedFoehn(ModuleBase module) : Components.UniformStackSpread(module, 6f, default, 8)
{

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FlintedFoehnP1AOE)
            ++NumCasts;
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.FlintedFoehn)
            AddStack(actor, World.FutureTime(5.1f));
    }
}

class P2FlintedFoehn(ModuleBase module) : Components.UniformStackSpread(module, 8f, default, 8)
{

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FlintedFoehnP2AOE)
            ++NumCasts;
    }

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.FlintedFoehn)
            AddStack(actor, World.FutureTime(5.1d));
    }
}
