// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN1TrinitySeeker;

sealed class DRN1TrinitySeekerStates : StateMachineBuilder
{
    public DRN1TrinitySeekerStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<MercifulBreeze>()
            .ActivateOnEnter<MercifulBlooms>()
            .ActivateOnEnter<MercifulArc>()
            .ActivateOnEnter<BurningChains>()
            .ActivateOnEnter<IronImpact>()
            .ActivateOnEnter<ActOfMercy>()
            .ActivateOnEnter<BalefulBlade>()
            .ActivateOnEnter<BalefulSwathe>()
            .ActivateOnEnter<IronSplitter>()
            .ActivateOnEnter<MercyFourfold>()
            .ActivateOnEnter<MercifulMoon>()
            .ActivateOnEnter<DeadIron>();
    }
}
