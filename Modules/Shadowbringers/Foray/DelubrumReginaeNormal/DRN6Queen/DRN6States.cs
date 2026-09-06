// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN6Queen;

sealed class DRN6QueenStates : StateMachineBuilder
{
    public DRN6QueenStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<Doom>()
            .ActivateOnEnter<NorthswainsGlowPawnOff>()
            .ActivateOnEnter<GodsSaveTheQueen>()
            .ActivateOnEnter<OptimalPlaySword>()
            .ActivateOnEnter<OptimalPlayShield>()
            .ActivateOnEnter<JudgmentBlade>()
            .ActivateOnEnter<HeavensWrathAOE>()
            .ActivateOnEnter<HeavensWrathKnockback>()
            .ActivateOnEnter<Chess>()
            .ActivateOnEnter<MeansEnds>()
            .ActivateOnEnter<TurretsTour>()
            .ActivateOnEnter<AboveBoard>()
            .ActivateOnEnter<CleansingSlash>();
    }
}
