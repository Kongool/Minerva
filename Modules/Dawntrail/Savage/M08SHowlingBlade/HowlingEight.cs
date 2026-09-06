// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M08SHowlingBlade;

sealed class Mooncleaver2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Mooncleaver2, 8f);

sealed class HowlingEight(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.HowlingEightFirst1 or (uint)AID.HowlingEightRest1)
        {
            Towers.Add(new(spell.LocXZ, 8f, 8, 8, activation: Module.CastFinishAt(spell)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.HowlingEightFirst1:
            case (uint)AID.HowlingEightRest1:
                ++NumCasts;
                break;
            case (uint)AID.HowlingEightFirst8:
            case (uint)AID.HowlingEightRest8:
                Towers.Clear();
                break;
        }
    }
}
