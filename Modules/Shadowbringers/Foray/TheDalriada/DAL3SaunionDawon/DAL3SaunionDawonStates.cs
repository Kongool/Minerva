// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.TheDalriada.DAL3SaunionDawon;

sealed class DAL3SaunionDawonStates : StateMachineBuilder
{
    public DAL3SaunionDawonStates(DAL3SaunionDawon module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MobileHaloCrossray>()
            .ActivateOnEnter<AntiPersonnelMissile>()
            .ActivateOnEnter<MagitekHalo>()
            .ActivateOnEnter<MagitekCrossray>()
            .ActivateOnEnter<MissileSalvo>()
            .ActivateOnEnter<SurfaceMissile>()
            .ActivateOnEnter<Touchdown>()
            .ActivateOnEnter<HighPoweredMagitekRay>()
            .Raw.Update = () => module.PrimaryActor.IsDestroyed || (module.BossDawon?.IsTargetable ?? false);
        TrivialPhase(1u)
            .ActivateOnEnter<VerdantPlumeVermilionFlame>()
            .ActivateOnEnter<FrigidPulse>()
            .ActivateOnEnter<SwoopingFrenzy>()
            .ActivateOnEnter<Pentagust>()
            .ActivateOnEnter<ToothAndTalon>()
            .ActivateOnEnter<WildfireWinds>()
            .ActivateOnEnter<Obey>()
            .ActivateOnEnter<SpiralScourge>()
            .ActivateOnEnter<OneMind>()
            .Raw.Update = () => module.PrimaryActor.IsDeadOrDestroyed && (module.BossDawon?.IsDeadOrDestroyed ?? true);
    }
}
