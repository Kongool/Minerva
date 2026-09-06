// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A34Promathia;

sealed class A34PromathiaStates : StateMachineBuilder
{
    public A34PromathiaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<EmptySalvation>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<WheelofImpregnability>()
            .ActivateOnEnter<BastionOfTwilight>()
            .ActivateOnEnter<PestilentPenance>()
            .ActivateOnEnter<Comet>()
            .ActivateOnEnter<FalseGenesis>()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<MemoryReceptacle>()
            .ActivateOnEnter<EmptyBeleaguer>()
            .ActivateOnEnter<AuroralDrape>()
            .ActivateOnEnter<WindsOfPromyvion>()
            .ActivateOnEnter<EmptySeed>()
            .ActivateOnEnter<DeadlyRebirth>()
            .ActivateOnEnter<MalevolentBlessingCone>()
            .ActivateOnEnter<MalevolentBlessingRect>()
            .ActivateOnEnter<PestilentPenanceLink>()
            .ActivateOnEnter<InfernalDeliveranceTower>()
            .ActivateOnEnter<InfernalDeliveranceAOE>()
            .ActivateOnEnter<Meteor>();
    }
}
