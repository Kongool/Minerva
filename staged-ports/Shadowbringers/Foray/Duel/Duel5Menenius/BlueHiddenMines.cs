// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel5Menenius;

sealed class BlueHiddenMines(ModuleBase module) : Components.GenericTowers(module)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if ((AID)spell.Action.ID is AID.ActivateBlueMine)
            Towers.Add(new(caster.Position, 3.6f));
        else if ((AID)spell.Action.ID is AID.DetonateBlueMine)
            Towers.RemoveAll(t => t.Position.AlmostEqual(caster.Position, 1));
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (Towers.Count > 0)
            hints.Add("Soak the mine!");
    }
}
