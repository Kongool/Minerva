// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A32HanselGretel;

[SkipLocalsInit]
sealed class A32HanselGretelStates : StateMachineBuilder
{
    public A32HanselGretelStates(A32HanselGretel module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<StrongerTogether>()
            .ActivateOnEnter<UpgradedShield>()
            .ActivateOnEnter<WailLamentation>()
            .ActivateOnEnter<CripplingBlow>()
            .ActivateOnEnter<BloodySweep>()
            .ActivateOnEnter<RiotOfMagicSeedOfMagicAlpha>()
            .ActivateOnEnter<PassingLance>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<UnevenFooting>()
            .ActivateOnEnter<HungryLance>()
            .ActivateOnEnter<Breakthrough>()
            .ActivateOnEnter<SeedOfMagicBeta>()
            .ActivateOnEnter<MagicalConfluence>()
            .Raw.Update = () => module.PrimaryActor.IsDeadOrDestroyed && (module.BossHansel?.IsDeadOrDestroyed ?? true);
    }
}
