// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A34UltimaP1;

class A34UltimaP1States : StateMachineBuilder
{
    public A34UltimaP1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HolyIVBait>()
            .ActivateOnEnter<HolyIVSpread>()
            .ActivateOnEnter<AuralightAOE>()
            .ActivateOnEnter<AuralightRect>()
            .ActivateOnEnter<GrandCrossAOE>()
            .ActivateOnEnter<TimeEruption>()
            .ActivateOnEnter<Eruption2>()
            .ActivateOnEnter<ControlTower2>()
            .ActivateOnEnter<ExtremeEdge1>()
            .ActivateOnEnter<ExtremeEdge2>()
            .ActivateOnEnter<CrushWeapon>()
            .ActivateOnEnter<Searchlight>()
            .ActivateOnEnter<HallowedBolt>();
    }
}
