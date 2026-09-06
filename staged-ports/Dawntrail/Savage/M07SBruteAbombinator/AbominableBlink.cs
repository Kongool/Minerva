// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M07SBruteAbombinator;

sealed class AbominableBlink(ModuleBase module) : Components.BaitAwayIcon(module, 24f, (uint)IconID.AbominableBlink, (uint)AID.AbominableBlink, 6.4d)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        var baits = ActiveBaitsOn(actor);
        if (baits.Count != 0)
        {
            hints.AddForbiddenZone(new SDRect(Center, new WDir(default, 1f), 24f, 24f, 25f), baits.Ref(0).Activation.AddSeconds(1d));
        }
    }
}
