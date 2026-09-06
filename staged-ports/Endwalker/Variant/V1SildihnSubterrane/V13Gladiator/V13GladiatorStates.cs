// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V13Gladiator;

sealed class V13GladiatorStates : StateMachineBuilder
{
    public V13GladiatorStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<SunderedRemains>()
            .ActivateOnEnter<BitingWindSmall>()
            .ActivateOnEnter<BitingWindUpdraft>()
            .ActivateOnEnter<BitingWindUpdraftVoidzone>()
            .ActivateOnEnter<RingOfMight1>()
            .ActivateOnEnter<RingOfMight2>()
            .ActivateOnEnter<RingOfMight3>()
            .ActivateOnEnter<RackAndRuin>()
            .ActivateOnEnter<RushOfMight>()
            .ActivateOnEnter<ShatteringSteelMeteor>()
            .ActivateOnEnter<FlashOfSteel>()
            .ActivateOnEnter<SculptorsPassion>()
            .ActivateOnEnter<GoldenFlame>()
            .ActivateOnEnter<SilverFlame>()
            .ActivateOnEnter<Landing>()
            .ActivateOnEnter<MightySmite>();
    }
}
