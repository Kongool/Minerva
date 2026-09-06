// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M03NBruteBomber;

sealed class M03NBruteBomberStates : StateMachineBuilder
{
    public M03NBruteBomberStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<KnuckleSandwich>()
            .ActivateOnEnter<BrutalImpact>()
            .ActivateOnEnter<BarbarousBarrageTower>()
            .ActivateOnEnter<BarbarousBarrageKnockback>()
            .ActivateOnEnter<BrutalLariat>()
            .ActivateOnEnter<ExplosiveRainCircle>()
            .ActivateOnEnter<ExplosiveRainConcentric>()
            .ActivateOnEnter<FireSpin>()
            .ActivateOnEnter<LitFuse>()
            .ActivateOnEnter<LariatCombo>()
            .ActivateOnEnter<BrutalBurn>()
            .ActivateOnEnter<MurderousMist>();
    }
}
