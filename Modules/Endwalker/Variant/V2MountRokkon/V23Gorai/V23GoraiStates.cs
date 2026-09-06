// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V23Gorai;

sealed class V23GoraiStates : StateMachineBuilder
{
    public V23GoraiStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            // Route 5
            .ActivateOnEnter<PureShock>()
            // Route 6
            .ActivateOnEnter<HumbleHammer>()
            .ActivateOnEnter<Thundercall>()
            // Route 7
            .ActivateOnEnter<WorldlyPursuit>()
            .ActivateOnEnter<FightingSpirits>()
            .ActivateOnEnter<BiwaBreaker>()
            // Standard
            .ActivateOnEnter<ImpurePurgation>()
            .ActivateOnEnter<StringSnap>()
            .ActivateOnEnter<SpikeOfFlameAOE>()
            .ActivateOnEnter<FlameAndSulphur>()
            .ActivateOnEnter<TorchingTorment>()
            .ActivateOnEnter<MalformedPrayer>()
            .ActivateOnEnter<Unenlightenment>();
    }
}
