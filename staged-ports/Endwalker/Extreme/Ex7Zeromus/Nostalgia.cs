// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex7Zeromus;

class NostalgiaDimensionalSurge(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.NostalgiaDimensionalSurge, 5);

class Nostalgia(ModuleBase module) : Components.CastCounter(module, default)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.NostalgiaBury1 or AID.NostalgiaBury2 or AID.NostalgiaBury3 or AID.NostalgiaBury4 or AID.NostalgiaRoar1 or AID.NostalgiaRoar2 or AID.NostalgiaPrimalRoar)
            ++NumCasts;
    }
}
