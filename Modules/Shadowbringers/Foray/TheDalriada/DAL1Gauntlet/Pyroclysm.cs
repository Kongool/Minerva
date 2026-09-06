// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL1Gauntlet;

sealed class Pyroclysm(ModuleBase module) : Components.GenericTowersOpenWorld(module)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.Pyroclysm or (uint)AID.Pyroplexy)
        {
            var count = Towers.Count;
            var pos = caster.Position;
            for (var i = 0; i < count; ++i)
            {
                if (Towers[i].Position.AlmostEqual(pos, 1f))
                {
                    Towers.RemoveAt(i);
                    return;
                }
            }
        }
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.TowerVisual1)
        {
            Towers.Add(new(actor.Position.Quantized(), 4f, 1, 1, activation: World.FutureTime(9d)));
        }
    }
}
