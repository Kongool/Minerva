// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A34XunZiMengZi;

sealed class A34XunZiMengZiStates : StateMachineBuilder
{
    public A34XunZiMengZiStates(A34XunZiMengZi module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<DeployArmaments>()
            .ActivateOnEnter<UniversalAssault>()
            .ActivateOnEnter<Energy>()
            .ActivateOnEnter<HighPoweredLaser>()
            .Raw.Update = () => module.PrimaryActor.IsDeadOrDestroyed && (module.BossMengZi?.IsDeadOrDestroyed ?? true);
    }
}
