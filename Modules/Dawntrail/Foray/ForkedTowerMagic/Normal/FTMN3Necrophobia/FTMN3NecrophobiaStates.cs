// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Normal.FTMN3Necrophobia;

[SkipLocalsInit]
sealed class NecrophobiaStates : StateMachineBuilder
{
    public NecrophobiaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<HailOfHellflares>()
            .ActivateOnEnter<AncientFire>()
            .ActivateOnEnter<AncientBlizzard>()
            .ActivateOnEnter<CorpseMangler>()
            .ActivateOnEnter<AncientThunder>()
            //.ActivateOnEnter<DarkCurrent1>()
            //.ActivateOnEnter<DarkCurrent2>()
            .ActivateOnEnter<DarkCurrent>()
            .ActivateOnEnter<DeathlyRay>()
            .ActivateOnEnter<VacuumWave>();
    }
}
