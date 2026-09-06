// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL4DiabloArmament;

sealed class DAL4DiabloArmamentStates : StateMachineBuilder
{
    public DAL4DiabloArmamentStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<RuinousPseudoomen>()
            .ActivateOnEnter<AethericBoomExplosionDiabolicGateVoidSystemsOverload>()
            .ActivateOnEnter<AdvancedNox>()
            .ActivateOnEnter<Aetheroplasm>()
            .ActivateOnEnter<AssaultCannon>()
            .ActivateOnEnter<DeadlyDealingKB>()
            .ActivateOnEnter<DeadlyDealingAOE>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<LightPseudopillar>()
            .ActivateOnEnter<PillarOfShamash>()
            .ActivateOnEnter<UltimatePseudoterror>()
            .ActivateOnEnter<AdvancedDeathIV>()
            .ActivateOnEnter<PillarOfShamashBait>()
            .ActivateOnEnter<PillarOfShamashStack>()
            .ActivateOnEnter<AccelerationBomb>();
    }
}
