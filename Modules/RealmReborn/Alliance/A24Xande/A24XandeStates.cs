// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A24Xande;

class A24XandeStates : StateMachineBuilder
{
    public A24XandeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<KnucklePress>()
            .ActivateOnEnter<BurningRave1>()
            .ActivateOnEnter<BurningRave2>()
            .ActivateOnEnter<AncientQuake>()
            .ActivateOnEnter<AncientQuaga>()
            //.ActivateOnEnter<Stackmarker>()
            .ActivateOnEnter<AuraCannon>();
    }
}
