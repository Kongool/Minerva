// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M07NBruteAbombinator;

class M07NBruteAbombinatorStates : StateMachineBuilder
{
    public M07NBruteAbombinatorStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<BrutalImpactRevengeOfTheVines1NeoBombarianSpecial>()
            .ActivateOnEnter<BrutishSwingCircle2>()
            .ActivateOnEnter<BrutishSwingDonut>()
            .ActivateOnEnter<BrutishSwingCone>()
            .ActivateOnEnter<BrutishSwingDonutSegment>()
            .ActivateOnEnter<NeoBombarianSpecialKB>()
            .ActivateOnEnter<SporeSac>()
            .ActivateOnEnter<Pollen>()
            .ActivateOnEnter<Powerslam>()
            .ActivateOnEnter<ItCameFromTheDirt>()
            .ActivateOnEnter<TheUnpotted>()
            .ActivateOnEnter<CrossingCrosswinds>()
            .ActivateOnEnter<CrossingCrosswindsHint>()
            .ActivateOnEnter<WindingWildwinds>()
            .ActivateOnEnter<WindingWildwindsHint>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<GlowerPower>()
            .ActivateOnEnter<ElectrogeneticForce>()
            .ActivateOnEnter<LashingLariat>()
            .ActivateOnEnter<Slaminator>()
            .ActivateOnEnter<PulpSmash>()
            .ActivateOnEnter<Sporesplosion>()
            .ActivateOnEnter<AbominableBlink>()
            .ActivateOnEnter<QuarrySwamp>()
            .ActivateOnEnter<BrutalSmashTB>();
    }
}
