// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A34Eulogia;

sealed class ByregotStrikeJump(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ByregotStrike, 8f);
sealed class ByregotStrikeKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ByregotStrikeKnockback, 20f)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            if (!IsImmune(slot, act))
            {
                hints.AddForbiddenZone(new SDKnockbackInCircleAwayFromOrigin(Center, c.Origin, 20f, 29f), act);
            }
        }
    }
}
sealed class ByregotStrikeCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ByregotStrikeAOE, new AOEShapeCone(90f, 22.5f.Degrees()));
