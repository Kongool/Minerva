// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A31KnaveofHearts;

sealed class A31KnaveofHeartsStates : StateMachineBuilder
{
    public A31KnaveofHeartsStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<Roar>()
            .ActivateOnEnter<ColossalImpact>()
            .ActivateOnEnter<MagicArtilleryBeta>()
            .ActivateOnEnter<MagicArtilleryAlpha>()
            .ActivateOnEnter<Energy>()
            .ActivateOnEnter<LightLeap>()
            .ActivateOnEnter<BoxSpawn>()
            .ActivateOnEnter<MagicBarrage>()
            .ActivateOnEnter<Lunge>();
    }
}
