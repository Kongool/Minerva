// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB3MarbleDragon;

sealed class VulnerabilityDown(ModuleBase module) : Components.Dispel(module, (uint)SID.VulnerabilityDown);
sealed class DamageUp(ModuleBase module) : Components.CastHint(module, (uint)AID.FrozenHeart, "Add enrage!");
sealed class IceGolems(ModuleBase module) : Components.Adds(module, (uint)OID.IceGolem, 1);
sealed class IceSprite(ModuleBase module) : Components.Adds(module, (uint)OID.IceSprite)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            if (e.Actor.FindStatus((uint)SID.Invincibility) != null)
            {
                e.Priority = AIHints.Enemy.PriorityInvincible;
            }
            else
            {
                e.Priority = 1;
            }
        }
    }
}

sealed class LifelessLegacy(ModuleBase module) : Components.RaidwideCastDelay(module, (uint)AID.LifelessLegacyVisual, (uint)AID.LifelessLegacy, 1.8f)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        var castInfo = Module.PrimaryActor.CastInfo;
        if (castInfo is ActorCastInfo info && info.RemainingTime < 6f)
            hints.Add(Hint);
    }
}
