// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Alliance.A33Cerberus;

class A33CerberusStates : StateMachineBuilder
{
    public A33CerberusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TailBlow>()
            .ActivateOnEnter<Slabber>()
            .ActivateOnEnter<Mini>()
            .ActivateOnEnter<SulphurousBreath1>()
            .ActivateOnEnter<SulphurousBreath2>()
            .ActivateOnEnter<LightningBolt2>()
            .ActivateOnEnter<HoundOutOfHell>()
            .ActivateOnEnter<Ululation>();
    }
}
