// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Heavensward.Alliance.A24Ozma;

[SkipLocalsInit]
sealed class A24OzmaStates : StateMachineBuilder
{
    public A24OzmaStates(ModuleBase module) : base(module)
    {
        bool IsWipedOrLeftRaid() => module.Raid.Player()!.Position is var p && !(p.InSquare(new(300f, 265f), 80f) || p.InSquare(new(280f, -404.5f), 40f))
        || module.World.CurrentCFCID != 168u;
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<MeteorImpact>()
            .ActivateOnEnter<HolyKB>()
            .ActivateOnEnter<Holy>()
            .ActivateOnEnter<ExecrationAOE>()
            .ActivateOnEnter<AccelerationBomb>()
            .Raw.Update = () => module.PrimaryActor.IsDestroyed && IsWipedOrLeftRaid();
    }
}
