// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C03AAI.C031Ketuduke;

class FlukeTyphoon(ModuleBase module) : Components.CastCounter(module, (uint)AID.FlukeTyphoonAOE);

class FlukeTyphoonBurst(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnMapEffect(byte index, uint state)
    {
        if (state == 0x00020001u)
        {
            WDir offset = index switch
            {
                0 => new(-10, -15),
                1 => new(-14, 0),
                2 => new(-10, +15),
                3 => new(+10, -15),
                4 => new(+14, 0),
                5 => new(+10, +15),
                _ => default
            };
            if (offset != default)
                Towers.Add(new(Center + offset, 4f));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.NBurst or (uint)AID.SBurst or (uint)AID.NBigBurst or (uint)AID.SBigBurst)
        {
            Towers.Clear();
            ++NumCasts;
        }
    }
}
