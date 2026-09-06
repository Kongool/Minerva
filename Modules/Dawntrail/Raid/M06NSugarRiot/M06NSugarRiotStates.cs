// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M06NSugarRiot;

sealed class M06NSugarRiotStates : StateMachineBuilder
{
    public M06NSugarRiotStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<SingleDoubleStyle>()
            .ActivateOnEnter<MousseMural>()
            .ActivateOnEnter<SprayPain>()
            .ActivateOnEnter<WarmBomb>()
            .ActivateOnEnter<CoolBomb>()
            .ActivateOnEnter<PuddingParty>()
            .ActivateOnEnter<MousseTouchUp>()
            .ActivateOnEnter<LightningBolt>()
            .ActivateOnEnter<TasteOfFire>()
            .ActivateOnEnter<TasteOfThunder>()
            .ActivateOnEnter<Quicksand>()
            .ActivateOnEnter<Highlightning>();
    }
}
