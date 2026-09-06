// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T05Necron;

sealed class T05NecronStates : StateMachineBuilder
{
    private Wipe? wipe;

    public T05NecronStates(ModuleBase module) : base(module)
    {
        bool IsWipedOrLeftRaid()
        {
            wipe ??= module.FindComponent<Wipe>();
            return (wipe?.Wiped ?? false) || module.World.CurrentCFCID != 1061u;
        }
        TrivialPhase()
            .ActivateOnEnter<Wipe>()
            .ActivateOnEnter<Prisons>()
            .ActivateOnEnter<FearOfDeathGrandCross>()
            .ActivateOnEnter<ChokingGrasp>()
            .ActivateOnEnter<FearOfDeathAOE>()
            .ActivateOnEnter<MementoMori>()
            .ActivateOnEnter<DarknessOfEternity>()
            .ActivateOnEnter<ColdGripExistentialDread>()
            .ActivateOnEnter<Aetherblight>()
            .ActivateOnEnter<FearOfDeathAOE2>()
            .ActivateOnEnter<GrandCrossArenaChange>()
            .ActivateOnEnter<NeutronRing>()
            .ActivateOnEnter<GrandCrossBait>()
            .ActivateOnEnter<GrandCrossRect>()
            .ActivateOnEnter<GrandCrossProx>()
            .ActivateOnEnter<BlueShockwave>()
            .ActivateOnEnter<Invitation>()
            .ActivateOnEnter<MassMacabre>()
            .ActivateOnEnter<SpreadingFear>()
            .Raw.Update = () => module.PrimaryActor.IsDead || IsWipedOrLeftRaid();
    }
}
