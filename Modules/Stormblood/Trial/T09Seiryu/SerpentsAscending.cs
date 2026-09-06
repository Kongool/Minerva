// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T09Seiryu;

sealed class SerpentAscending(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.Tower)
        {
            Towers.Add(new(actor.Position.Quantized(), 3f, activation: World.FutureTime(7.8d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.SerpentsFang or (uint)AID.SerpentsJaws)
        {
            Towers.Clear();
        }
    }
}
