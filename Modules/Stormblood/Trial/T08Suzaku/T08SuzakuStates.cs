// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T08Suzaku;

class T08SuzakuStates : StateMachineBuilder
{
    public T08SuzakuStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ScreamsOfTheDamned>()
            .ActivateOnEnter<SouthronStar>()
            .ActivateOnEnter<AshesToAshes>()
            .ActivateOnEnter<ScarletFeverAOE>()
            .ActivateOnEnter<RuthlessRefrain>()
            .ActivateOnEnter<Cremate>()
            .ActivateOnEnter<PhantomFlurryTankbuster>()
            .ActivateOnEnter<PhantomFlurryAOE>()
            .ActivateOnEnter<FleetingSummer>()
            .ActivateOnEnter<Hotspot>()
            .ActivateOnEnter<Swoop>()
            .ActivateOnEnter<WellOfFlame>()
            .ActivateOnEnter<ScarletFever>();
    }
}
