// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M06SSugarRiot;

sealed class StickyMousse(ModuleBase module) : Components.GenericStackSpread(module, true)
{
    public int NumCasts;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.StickyMousseVisual)
        {
            var party = Raid.WithoutSlot(false, true, true);
            var len = party.Length;
            var act = Module.CastFinishAt(spell, 0.8d);
            for (var i = 0; i < len; ++i)
            {
                var p = party[i];
                if (p.Role != Role.Tank) // never targets tanks unless pretty much everyone is dead, but then there are worse issues
                {
                    Spreads.Add(new(p, 4f, act));
                }
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.StickyMousse:
                Spreads.Clear();
                Stacks.Add(new(World.Actors.Find(spell.MainTargetID)!, 4f, 4, 4, World.FutureTime(6d)));
                break;
            case (uint)AID.BurstStickyMousse:
                ++NumCasts;
                break;

        }
    }
}
