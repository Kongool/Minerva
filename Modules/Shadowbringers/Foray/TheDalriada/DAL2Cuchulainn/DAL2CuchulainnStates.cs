// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL2Cuchulainn;

sealed class DAL2CuchulainnStates : StateMachineBuilder
{
    public DAL2CuchulainnStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<FleshNecromass>()
            .ActivateOnEnter<FleshNecromassJumps>()
            .ActivateOnEnter<FellFlowAOE>()
            .ActivateOnEnter<FellFlowBait>()
            .ActivateOnEnter<GhastlyAura>()
            .ActivateOnEnter<AmbientPulsation>()
            .ActivateOnEnter<NecroticBillow>()
            .ActivateOnEnter<BurgeoningDread>()
            .ActivateOnEnter<MightOfMalice>()
            .ActivateOnEnter<PutrifiedSoulBurgeoningDreadGhastlyAura>();
    }
}
