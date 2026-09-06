// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A21FaithboundKirin;

sealed class A21FaithboundKirinStates : StateMachineBuilder
{
    public A21FaithboundKirinStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<Punishment>()
            .ActivateOnEnter<CrimsonRiddle>()
            .ActivateOnEnter<StonegaIII1>()
            .ActivateOnEnter<StonegaIII2>()
            .ActivateOnEnter<StonegaIVShatteringStomp>()
            .ActivateOnEnter<QuakeSmall>()
            .ActivateOnEnter<QuakeBig>()
            .ActivateOnEnter<EastwindWheel>()
            .ActivateOnEnter<SynchronizedStrikeSmite>()
            .ActivateOnEnter<Wringer>()
            .ActivateOnEnter<StrikingSmiting>()
            .ActivateOnEnter<DeadlyHold>()
            .ActivateOnEnter<Bury>()
            .ActivateOnEnter<Shockwave>()
            .ActivateOnEnter<KirinCaptivator>()
            .ActivateOnEnter<WallArenaChange>()
            .ActivateOnEnter<GloamingGleam>()
            .ActivateOnEnter<RazorFang>()
            .ActivateOnEnter<VermilionFlight>()
            .ActivateOnEnter<ArmOfPurgatory>()
            .ActivateOnEnter<MoontideFont>()
            .ActivateOnEnter<MidwinterMarchNorthernCurrent>();
    }
}
