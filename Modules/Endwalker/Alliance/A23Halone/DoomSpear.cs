// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A23Halone;

sealed class DoomSpear(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DoomSpearAOE1 or (uint)AID.DoomSpearAOE2 or (uint)AID.DoomSpearAOE3)
        {
            Towers.Add(new(spell.LocXZ, 6f, 8, 8, default, Module.CastFinishAt(spell), caster.InstanceID));

        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.DoomSpearAOE1 or (uint)AID.DoomSpearAOE2 or (uint)AID.DoomSpearAOE3)
        {
            var towers = CollectionsMarshal.AsSpan(Towers);
            var len = towers.Length;
            var id = caster.InstanceID;
            ++NumCasts;
            for (var i = 0; i < len; ++i)
            {
                if (towers[i].ActorID == id)
                {
                    Towers.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
