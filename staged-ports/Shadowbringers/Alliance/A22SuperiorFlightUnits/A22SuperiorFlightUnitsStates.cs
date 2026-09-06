// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A22SuperiorFlightUnits;

sealed class A22SuperiorFlightUnitsStates : StateMachineBuilder
{
    public A22SuperiorFlightUnitsStates(A22SuperiorFlightUnits module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ShieldProtocol>()
            .ActivateOnEnter<IncendiaryBombing>()
            .ActivateOnEnter<IncendiaryBombingBait>()
            .ActivateOnEnter<StandardSurfaceMissile>()
            .ActivateOnEnter<LethalRevolution>()
            .ActivateOnEnter<GuidedMissile>()
            .ActivateOnEnter<SurfaceMissileHighOrderExplosiveBlastCircle>()
            .ActivateOnEnter<HighOrderExplosiveBlastCross>()
            .ActivateOnEnter<AntiPersonnelMissile>()
            .ActivateOnEnter<PrecisionGuidedMissile>()
            .ActivateOnEnter<ManeuverHighPoweredLaser>()
            .ActivateOnEnter<ManeuverMissileCommand>()
            .ActivateOnEnter<SharpTurn>()
            .ActivateOnEnter<SlidingSwipe>()
            .ActivateOnEnter<IncendiaryBarrage>()
            .Raw.Update = () => module.PrimaryActor.IsDeadOrDestroyed && (module.BossBeta?.IsDeadOrDestroyed ?? true) && (module.BossChi?.IsDeadOrDestroyed ?? true);
    }
}
