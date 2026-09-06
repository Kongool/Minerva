// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Modules.Dawntrail.Advanced.Ad01TheMerchantsTale.Ad011PariofPlenty;

sealed class PariOfPlentyStates : StateMachineBuilder
{
    public PariOfPlentyStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HeatBurst>()
            .ActivateOnEnter<FireFlight>()
            .ActivateOnEnter<BurningGleam>()
            .ActivateOnEnter<CharmedChains>()
            .ActivateOnEnter<SimpleFableFlight>()
            .ActivateOnEnter<FireOfVictory>()
            .ActivateOnEnter<LeftRightFireflight>()
            .ActivateOnEnter<WheelOfFireflight>()
            .ActivateOnEnter<FellSpark>()
            .ActivateOnEnter<CurseOfCompanionshipSolitude>()
            .ActivateOnEnter<DoubleFableFlight>()
            .ActivateOnEnter<SpurningFlames>()
            .ActivateOnEnter<ImpassionedSpark>()
            .ActivateOnEnter<SparkPuddle>()
            .ActivateOnEnter<BurningPillar>()
            .ActivateOnEnter<FireWell>()
            .ActivateOnEnter<ScouringScorn>()
            .ActivateOnEnter<FireFlightFactOrFiction>()
            .ActivateOnEnter<FalseFlameDisplay>();
    }
}
