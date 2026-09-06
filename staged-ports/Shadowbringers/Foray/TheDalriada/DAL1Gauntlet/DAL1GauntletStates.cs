// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL1Gauntlet;

sealed class DAL1GauntletStates : StateMachineBuilder
{
    public DAL1GauntletStates(DAL1Gauntlet module) : base(module)
    {

        TrivialPhase(default)
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<BallisticImpact>()
            .ActivateOnEnter<SurfaceMissile>()
            .ActivateOnEnter<TerminusEst>()
            .ActivateOnEnter<TerminusEstUnseen>()
            .ActivateOnEnter<CeruleumExplosion>()
            .Raw.Update = () => module.PrimaryActor.IsDestroyed || (module.BossAugur?.IsTargetable ?? false);
        TrivialPhase(1u)
            .ActivateOnEnter<Pyroclysm>()
            .ActivateOnEnter<SeventyFourDegrees>()
            .ActivateOnEnter<FlamingCyclone>()
            .ActivateOnEnter<Turbine>()
            .ActivateOnEnter<SanctifiedQuakeIII>()
            .Raw.Update = () => (module.BossAugur?.IsDestroyed ?? true) || (module.BossAlkonost?.IsTargetable ?? false);
        TrivialPhase(2u)
            .ActivateOnEnter<Stormcall>()
            .ActivateOnEnter<NorthSouthwind>()
            .ActivateOnEnter<NihilitysSong>()
            .ActivateOnEnter<PainStormFrigidPulsePainfulGust>()
            .ActivateOnEnter<BroadsideBarrage>()
            .Raw.Update = () => (module.BossAlkonost?.IsDeadOrDestroyed ?? true) && (module.BossCrow?.IsDeadOrDestroyed ?? true);
    }
}
