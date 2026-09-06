// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Alliance.A35UltimaP2;

class A35UltimaP2States : StateMachineBuilder
{
    public A35UltimaP2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HolyIVBait>()
            .ActivateOnEnter<HolyIVSpread>()
            .ActivateOnEnter<Redemption>()
            .ActivateOnEnter<Auralight1>()
            .ActivateOnEnter<Auralight2>()
            .ActivateOnEnter<Bombardment>()
            .ActivateOnEnter<Embrace2>()
            .ActivateOnEnter<GrandCrossAOE>()
            .ActivateOnEnter<Holy>()
            .ActivateOnEnter<HolyIVBait>()
            .ActivateOnEnter<HolyIVSpread>()
            .ActivateOnEnter<Plummet>()
            .ActivateOnEnter<Cataclysm>();
    }
}
