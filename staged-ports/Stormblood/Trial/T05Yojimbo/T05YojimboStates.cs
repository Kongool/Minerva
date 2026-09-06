// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class T05YojimboStates : StateMachineBuilder
{
    public T05YojimboStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Fragility>()
            .ActivateOnEnter<MettaGiri>()
            .ActivateOnEnter<Yukikaze2>()
            .ActivateOnEnter<Gekko2>()
            .ActivateOnEnter<Kasha2>()
            .ActivateOnEnter<TinySong>()
            .ActivateOnEnter<DragonsLair>()
            .ActivateOnEnter<BitterEnd2>()
            .ActivateOnEnter<DragonNight>()
            .ActivateOnEnter<GigaJump>()
            .ActivateOnEnter<AmeNoMurakumo>()
            .ActivateOnEnter<ElectrogeneticForce2>()
            .ActivateOnEnter<Enchain>()
            .ActivateOnEnter<HellsGate>()
            .ActivateOnEnter<Masamune>()
            .ActivateOnEnter<ZanmaZanmai>()
            .ActivateOnEnter<EpicStormsplitter>();
    }
}
