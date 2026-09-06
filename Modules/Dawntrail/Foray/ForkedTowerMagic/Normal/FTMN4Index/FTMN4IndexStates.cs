// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Normal.FTMN4Index;

[SkipLocalsInit]
sealed class IndexStates : StateMachineBuilder
{
    public IndexStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<OmniElementPanels>()
            .ActivateOnEnter<Flare>()
            .ActivateOnEnter<Aim>()
            .ActivateOnEnter<RomeosBallad>()
            .ActivateOnEnter<ElementaryEvocation>()
            .ActivateOnEnter<ElementaryExpansion>()
            .ActivateOnEnter<ElementaryChemistry>()
            .ActivateOnEnter<Shockwave>()
            .ActivateOnEnter<Bombs>()
            .ActivateOnEnter<DuologyOfImplements>()
            .ActivateOnEnter<AllConsumingFlames>()
            .ActivateOnEnter<Predict>();
    }
}
