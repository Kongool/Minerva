// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T04Zelenia;

sealed class T04ZeleniaStates : StateMachineBuilder
{
    public T04ZeleniaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<AlexandrianThunderIV>()
            .ActivateOnEnter<AlexandrianThunderIII>()
            .ActivateOnEnter<ShockSpread>()
            .ActivateOnEnter<ShockAOE>()
            .ActivateOnEnter<PowerBreak>()
            .ActivateOnEnter<HolyHazard>()
            .ActivateOnEnter<SpecterOfTheLost>()
            .ActivateOnEnter<ThunderSlash>()
            .ActivateOnEnter<RosebloodBloom>()
            .ActivateOnEnter<PerfumedQuietus>()
            .ActivateOnEnter<ValorousAscension>()
            .ActivateOnEnter<ThornedCatharsis>()
            .ActivateOnEnter<StockBreak>()
            .ActivateOnEnter<ValorousAscensionRect>();
    }
}
