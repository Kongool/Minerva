// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Extreme.Ex8Enuo;

sealed class MeltdownAoE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Meltdown1, new AOEShapeCircle(5f));

sealed class MeltdownSpread(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.Meltdown2, 5f);

sealed class MeltdownWait(ModuleBase module) : Components.StayMove(module)
{
    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ChainsOfCondemnation)
        {
            PlayerStates[Raid.FindSlot(actor.InstanceID)] = new(Requirement.Stay2, World.CurrentTime, finish: status.ExpireAt);
        }
    }
    public override void OnStatusLose(Actor actor, ref ActorStatus status)
    {
        if (status.ID == (uint)SID.ChainsOfCondemnation)
        {
            PlayerStates[Raid.FindSlot(actor.InstanceID)] = default;
        }
    }
}
