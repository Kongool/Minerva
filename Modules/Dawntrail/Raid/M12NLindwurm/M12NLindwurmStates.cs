// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M12NLindwurm;

[SkipLocalsInit]
sealed class M12NLindwurmStates : StateMachineBuilder
{
    public M12NLindwurmStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<TheFixer>()
            .ActivateOnEnter<SerpentineScourge>()
            .ActivateOnEnter<RavenousReach>()
            .ActivateOnEnter<Burst>()
            .ActivateOnEnter<VisceralBurst>()
            .ActivateOnEnter<Splattershed>()
            .ActivateOnEnter<BringDownTheHouse>()
            .ActivateOnEnter<SplitScourge>()
            .ActivateOnEnter<VenomousScourge>()
            .ActivateOnEnter<GrandEntrance>()
            .ActivateOnEnter<MindlessFlesh>()
            .ActivateOnEnter<MindlessFleshBig>()
            .ActivateOnEnter<FleshTele>()
            .ActivateOnEnter<CruelCoil>()
            .ActivateOnEnter<BurstingGrotesquerie>()
            .ActivateOnEnter<SharedGrotesquerie>()
            .ActivateOnEnter<DirectedGrotesquerie>();
    }
}
