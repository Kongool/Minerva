// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A30Shantoto;

sealed class A30ShantotoStates : StateMachineBuilder
{
    public A30ShantotoStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FlarePlay>()
            .ActivateOnEnter<Vidohunir>()
            .ActivateOnEnter<EmpiricalResearch>()
            .ActivateOnEnter<SuperiorStoneIITelegraph>()
            .ActivateOnEnter<SuperiorStoneIIArena>()
            .ActivateOnEnter<GroundBreakingQuake>()
            .ActivateOnEnter<DiagrammaticDoorway>()
            .ActivateOnEnter<LocalizedBlizzard>()
            .ActivateOnEnter<ThunderAndError>()
            .ActivateOnEnter<SmallSpecimen>()
            .ActivateOnEnter<LargeSpecimen>()
            .ActivateOnEnter<StardustSpecimen>()
            .ActivateOnEnter<Shockwave>()
            .ActivateOnEnter<FallingRubble>()
            .ActivateOnEnter<FallingRubble1>()
            .ActivateOnEnter<FallingRubble2>()
            .ActivateOnEnter<FallingRubble3>()
            .ActivateOnEnter<AeroDynamics>()
            .ActivateOnEnter<FinalExam>();
    }
}
