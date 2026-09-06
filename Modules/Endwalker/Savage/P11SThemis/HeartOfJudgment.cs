// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P11SThemis;

class HeartOfJudgment : Components.GenericTowers
{
    public HeartOfJudgment(ModuleBase module) : base(module)
    {
        for (var i = 0; i < 4; ++i)
            Towers.Add(new(Center + 11.5f * (45 + i * 90).Degrees().ToDirection(), 4, 2, 2));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.Explosion or AID.MassiveExplosion)
        {
            ++NumCasts;
            Towers.Clear();
        }
    }
}
