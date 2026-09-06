// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.DelubrumReginae.DRN3QueensGuard;

sealed class DRN3QueensGuardStates : StateMachineBuilder
{
    public DRN3QueensGuardStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<BloodAndBoneQueenShotUnseen>()
            .ActivateOnEnter<RapidSeverShotInTheDark>()
            .ActivateOnEnter<Enrages>()
            .ActivateOnEnter<OptimalPlaySword>()
            .ActivateOnEnter<OptimalPlayShield>()
            .ActivateOnEnter<CoatOfArms>()
            .ActivateOnEnter<AboveBoard>()
            .ActivateOnEnter<TurretsTour>()
            .ActivateOnEnter<PawnOff>();
    }
}
