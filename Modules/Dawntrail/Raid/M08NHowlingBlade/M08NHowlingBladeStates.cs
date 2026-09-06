// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M08NHowlingBlade;

sealed class M08NHowlingBladeStates : StateMachineBuilder
{
    public M08NHowlingBladeStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<ExtraplanarTitanicPursuit>()
            .ActivateOnEnter<GreatDivide>()
            .ActivateOnEnter<Heavensearth1>()
            .ActivateOnEnter<Heavensearth2>()
            .ActivateOnEnter<WolvesReignRect1>()
            .ActivateOnEnter<WolvesReignRect2>()
            .ActivateOnEnter<WolvesReignCone>()
            .ActivateOnEnter<WolvesReignCircle>()
            .ActivateOnEnter<MoonbeamsBite>()
            .ActivateOnEnter<RoaringWindShadowchase>()
            .ActivateOnEnter<TargetedQuake>()
            .ActivateOnEnter<FangedCharge>()
            .ActivateOnEnter<TerrestrialTitans>()
            .ActivateOnEnter<Towerfall>()
            .ActivateOnEnter<TrackingTremors>()
            .ActivateOnEnter<RavenousSaber>()
            .ActivateOnEnter<Gust>()
            .ActivateOnEnter<GrowlingWindWealofStone>();
    }
}
