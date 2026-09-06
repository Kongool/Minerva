// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN5TrinityAvowed;

sealed class DRN5TrinityAvowedStates : StateMachineBuilder
{
    public DRN5TrinityAvowedStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<PlayerTemperatures>()
            .ActivateOnEnter<GloryOfBozja>()
            .ActivateOnEnter<WrathOfBozja>()
            .ActivateOnEnter<ElementalImpact>()
            .ActivateOnEnter<ElementalImpactTemperature>()
            .ActivateOnEnter<ShimmeringShot>()
            .ActivateOnEnter<AllegiantArsenal>()
            .ActivateOnEnter<BladeOfEntropy>()
            .ActivateOnEnter<GleamingArrow>();
    }
}