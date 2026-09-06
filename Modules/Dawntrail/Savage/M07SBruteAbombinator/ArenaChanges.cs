// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;
using static Minerva.Dawntrail.Raid.BruteAmbombinatorSharedBounds.BruteAmbombinatorSharedBounds;


namespace Minerva.Dawntrail.Savage.M07SBruteAbombinator;

sealed class ArenaChanges(ModuleBase module) : ModuleComponent(module)
{
    public override void OnMapEffect(byte index, uint state)
    {
        if (state == 0x00020001u)
        {
            if (index == 0x00)
            {
                Bounds = RectArena;
                Center = FinalCenter;
            }
            else if (index == 0x01)
            {
                Bounds = DefaultArena;
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.NeoBombarianSpecial)
        {
            Bounds = KnockbackArena;
            Center = KnockbackArena.Center;
        }
    }
}
