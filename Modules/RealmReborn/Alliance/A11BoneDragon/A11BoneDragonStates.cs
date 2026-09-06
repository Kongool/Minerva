// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A11BoneDragon;

class A11BoneDragonStates : StateMachineBuilder
{
    public A11BoneDragonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Apocalypse>()
            .ActivateOnEnter<EvilEye>()
            .ActivateOnEnter<Stone>()
            .ActivateOnEnter<Level5Petrify>()
            .Raw.Update = () => Module.PrimaryActor.IsDestroyed;
    }
}
