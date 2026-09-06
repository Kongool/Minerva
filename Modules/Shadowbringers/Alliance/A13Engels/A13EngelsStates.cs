// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A13Engels;

class A13MarxEngelsStates : StateMachineBuilder
{
    public A13MarxEngelsStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DemolishStructureArenaChange>()

            .ActivateOnEnter<PrecisionGuidedMissile2>()
            .ActivateOnEnter<DiffuseLaser>()
            .ActivateOnEnter<LaserSight1>()
            .ActivateOnEnter<GuidedMissile2>()
            .ActivateOnEnter<IncendiaryBombing2>()
            //.ActivateOnEnter<IncendiaryBombing1>()
            .ActivateOnEnter<GuidedMissile>()
            .ActivateOnEnter<DiffuseLaser>()
            .ActivateOnEnter<MarxSmash1>()
            .ActivateOnEnter<MarxSmash2>()
            .ActivateOnEnter<MarxSmash3>()
            .ActivateOnEnter<MarxSmash4>()
            .ActivateOnEnter<MarxSmash5>()
            .ActivateOnEnter<MarxSmash6>()
            .ActivateOnEnter<MarxSmash7>()
            .ActivateOnEnter<MarxCrush>()
            .ActivateOnEnter<SurfaceMissile2>();
    }
}
