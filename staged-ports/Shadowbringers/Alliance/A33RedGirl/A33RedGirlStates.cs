// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Alliance.A33RedGirl;

sealed class A33RedGirlStates : StateMachineBuilder
{
    public A33RedGirlStates(A33RedGirl module) : base(module)
    {
        bool IsWipedOrLeftRaid() => module.Raid.Player()!.Position is var p && !(p.InSquare(new(default, 900f), 44f) || p.InSquare(new(default, 400f), 44f)
            || p.InSquare(new(default, -100f), 44f) || p.InSquare(new(845f, -851f), 25f)) || module.World.CurrentCFCID != 779u;
        TrivialPhase()
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<WipeBlackWhite>()
            .ActivateOnEnter<CrueltyP1>()
            .ActivateOnEnter<PointBlackWhite>()
            .ActivateOnEnter<ShockWhiteBaitSlow>()
            .ActivateOnEnter<ShockWhiteBaitFast>()
            .ActivateOnEnter<ShockBlackBait>()
            .ActivateOnEnter<SublimeTranscendence>()
            .ActivateOnEnter<GenerateBarrier1>()
            .ActivateOnEnter<GenerateBarrier2>()
            .ActivateOnEnter<ShockWhiteBlack>()
            .ActivateOnEnter<ManipulateEnergy>()
            .ActivateOnEnter<DiffuseEnergy>()
            .Raw.Update = () => module.PrimaryActor.IsDestroyed && IsWipedOrLeftRaid() || module.RedSphere != null;
        TrivialPhase(1u)
            .ActivateOnEnter<IntermissionArena>()
            .ActivateOnEnter<IntermissionAIModule>()
            .ActivateOnEnter<WaveWhite>()
            .ActivateOnEnter<WaveBlack>()
            .ActivateOnEnter<BigExplosion>()
            .Raw.Update = () => (module.RedSphere?.IsDestroyed ?? true) && IsWipedOrLeftRaid() || (module.BossP2?.IsTargetable ?? false);
        TrivialPhase(2u)
            .OnEnter(() => module.Center = new(845f, -851f))
            .OnEnter(() => module.Bounds = new ArenaBoundsSquare(24.5f))
            .ActivateOnEnter<ArenaChanges>()
            .ActivateOnEnter<WipeBlackWhite>()
            .ActivateOnEnter<ShockBlackBait>()
            .ActivateOnEnter<ShockWhiteBaitSlow>()
            .ActivateOnEnter<PointBlackWhite>()
            .ActivateOnEnter<CrueltyP2>()
            .ActivateOnEnter<Explosion>()
            .ActivateOnEnter<GenerateBarrier2>()
            .ActivateOnEnter<GenerateBarrier3>()
            .ActivateOnEnter<GenerateBarrier4>()
            .ActivateOnEnter<ManipulateEnergy>()
            .ActivateOnEnter<DiffuseEnergy>()
            .ActivateOnEnter<ChildsPlay>()
            .Raw.Update = () => (module.BossP2?.IsDestroyed ?? true) || module.BossP2?.HPMP.CurHP <= 1u;
    }
}
