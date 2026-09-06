// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A32FiveheadedDragon;

class A32FiveheadedDragonStates : StateMachineBuilder
{
    public A32FiveheadedDragonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<WhiteBreath>()
            .ActivateOnEnter<BreathOfFire>()
            .ActivateOnEnter<BreathOfLight>()
            .ActivateOnEnter<BreathOfPoison>()
            .ActivateOnEnter<BreathOfIce>()
            .ActivateOnEnter<Radiance>()
            .ActivateOnEnter<HeatWave>();
    }
}
