// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.C01ASS.C012Gladiator;

abstract class SculptorsPassion(ModuleBase module, uint aid) : Components.GenericWildCharge(module, 4f, aid)
{
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == (uint)AID.SculptorsPassionTargetSelection)
        {
            Source = Module.PrimaryActor;
            foreach (var (slot, player) in Raid.WithSlot(true, true, true))
                PlayerRoles[slot] = player.InstanceID == spell.MainTargetID ? PlayerRole.Target : player.Role == Role.Tank ? PlayerRole.Share : PlayerRole.ShareNotFirst;
        }
    }
}
sealed class NSculptorsPassion(ModuleBase module) : SculptorsPassion(module, (uint)AID.NSculptorsPassion);
sealed class SSculptorsPassion(ModuleBase module) : SculptorsPassion(module, (uint)AID.SSculptorsPassion);
