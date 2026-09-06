// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A23HeavyArtilleryUnit;

sealed class ChemicalBurn(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (state == 0x00010002u && actor.OID == (uint)OID.Tower)
        {
            Towers.Add(new(actor.Position.Quantized(), 3, 3, activation: World.FutureTime(20d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ChemicalBurn)
        {
            Towers.Clear();
        }
    }
}
