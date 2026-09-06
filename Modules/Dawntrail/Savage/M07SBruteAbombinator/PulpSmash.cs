// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M07SBruteAbombinator;

sealed class PulpSmash(ModuleBase module) : Components.StackWithIcon(module, (uint)IconID.PulpSmash, (uint)AID.PulpSmash, 6f, 5.1f, 8, 8);
sealed class ItCameFromTheDirt(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ItCameFromTheDirt, 6f);

sealed class TheUnpotted(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.TheUnpotted)
{
    private static readonly AOEShapeCone cone = new(60f, 15f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ItCameFromTheDirt)
        {
            var act = Module.CastFinishAt(spell, 0.1d);
            var party = Raid.WithoutSlot(false, true, true);
            var source = Module.PrimaryActor;
            var len = party.Length;

            for (var i = 0; i < len; ++i)
            {
                CurrentBaits.Add(new(source, party[i], cone, act));
            }
        }
    }
}
