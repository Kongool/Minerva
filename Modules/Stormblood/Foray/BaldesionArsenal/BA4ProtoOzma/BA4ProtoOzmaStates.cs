// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.BaldesionArsenal.BA4ProtoOzma;

sealed class BA4ProtoOzmaStates : StateMachineBuilder
{
    public BA4ProtoOzmaStates(ModuleBase module) : base(module)
    {
        DeathPhase(0, SinglePhase)
            .ActivateOnEnter<TransitionAttacks>()
            .ActivateOnEnter<AutoAttacksCube>()
            .ActivateOnEnter<AutoAttacksPyramid>()
            .ActivateOnEnter<AutoAttacksStar>()
            .ActivateOnEnter<BlackHole>()
            .ActivateOnEnter<Tornado>()
            .ActivateOnEnter<ShootingStar>()
            .ActivateOnEnter<MeteorStack>()
            .ActivateOnEnter<MeteorBait>()
            .ActivateOnEnter<MeteorImpact>()
            .ActivateOnEnter<Ozmaspheres>()
            .ActivateOnEnter<AccelerationBomb>()
            .ActivateOnEnter<Holy>();
    }

    private void SinglePhase(uint id)
    {
        SimpleState(id + 0xFF0000, 10000, "???");
    }
    //TODO: implement
    //private void XXX(uint id, float delay)
}