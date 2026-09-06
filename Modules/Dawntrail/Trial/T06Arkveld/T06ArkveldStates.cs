// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T06Arkveld;

[SkipLocalsInit]
sealed class GuardianArkveldStates : StateMachineBuilder
{
    public GuardianArkveldStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            // Raidwides
            .ActivateOnEnter<Roar>()
            .ActivateOnEnter<ForgedFury>()
            // Major tells
            .ActivateOnEnter<ChainbladeBlowLines>()
            .ActivateOnEnter<WyvernsRadianceCleave>()
            .ActivateOnEnter<GuardianResonanceRect>()
            .ActivateOnEnter<Rush>()
            .ActivateOnEnter<Concentric1>()
            .ActivateOnEnter<Concentric2>()
            .ActivateOnEnter<Concentric3>()
            .ActivateOnEnter<Concentric4>()
            .ActivateOnEnter<WyvernsOuroblade>()
            .ActivateOnEnter<SteeltailThrust>()
            // Player mechanics
            .ActivateOnEnter<WildEnergy>()
            .ActivateOnEnter<ChainbladeCharge>()
            .ActivateOnEnter<ResonanceTowerSmall>()
            .ActivateOnEnter<ResonanceTowerLarge>()
            // Arena hazards
            .ActivateOnEnter<CrackedCrystalSmall>()
            .ActivateOnEnter<CrackedCrystalLarge>()
            .ActivateOnEnter<WyvernsVengeance>()
            .ActivateOnEnter<WyvernsWealAOE>()  // the casted rect telegraph
            .ActivateOnEnter<WyvernsWealPulses>()
            .ActivateOnEnter<WyvernsWealIrregularCastLane>();
    }
}
