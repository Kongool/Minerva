// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Extreme.Ex7Zeromus;

class SableThread(ModuleBase module) : Components.GenericWildCharge(module, 6, (uint)AID.SableThreadAOE, 60)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if ((AID)spell.Action.ID == AID.SableThreadTarget)
        {
            Source = caster;
            foreach (var (i, p) in Raid.WithSlot(true, true, true))
                PlayerRoles[i] = p.InstanceID == spell.MainTargetID ? PlayerRole.Target : p.Role == Role.Tank ? PlayerRole.Share : PlayerRole.ShareNotFirst;
        }
    }
}
