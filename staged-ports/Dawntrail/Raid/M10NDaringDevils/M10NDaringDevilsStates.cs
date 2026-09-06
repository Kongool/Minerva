// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.DawnTrail.Raid.M10NDaringDevils;

[SkipLocalsInit]
sealed class M10NDaringDevilsStates : StateMachineBuilder
{
    private readonly M10NDaringDevils _module;

    public M10NDaringDevilsStates(M10NDaringDevils module) : base(module)
    {
        _module = module;

        TrivialPhase()
            .ActivateOnEnter<HotImpact>()
            .ActivateOnEnter<DeepImpact>()
            .ActivateOnEnter<DiversDare>()
            .ActivateOnEnter<DiversDareBlue>()

            .ActivateOnEnter<CutbackBlazeBait>()
            .ActivateOnEnter<CutbackBlazePersistent>()

            .ActivateOnEnter<AlleyOopInfernoSpread>()
            .ActivateOnEnter<AlleyOopInfernoPuddles>()
            .ActivateOnEnter<AlleyOopMaelstromSequential>()
            .ActivateOnEnter<SteamBurst>()
            .ActivateOnEnter<HotAerialTowers>()
            .ActivateOnEnter<HotAerialFirePuddles>()


            .ActivateOnEnter<DeepVarialCone>()
            .ActivateOnEnter<SickestTakeOffLine>()
            .ActivateOnEnter<SickSwellKB>()

            .ActivateOnEnter<PyrotationStack>()
            .ActivateOnEnter<PyrotationPuddles>()
            .ActivateOnEnter<XtremeSpectacularRaidwide>()
            .ActivateOnEnter<XtremeSpectacularEdge>()

            .ActivateOnEnter<InsaneAirSnaps>()
            .ActivateOnEnter<BlastingSnapPersistent>()


            .Raw.Update = () => Module.PrimaryActor.IsDeadOrDestroyed && (_module.DeepBlue?.IsDeadOrDestroyed ?? true);
    }
}
