// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel6Lyon;

sealed class Duel6LyonStates : StateMachineBuilder
{
    public Duel6LyonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<OnFire>()
            .ActivateOnEnter<WildfiresFury>()
            .ActivateOnEnter<HeavenAndEarth>()
            .ActivateOnEnter<CagedHeartOfNature>()
            .ActivateOnEnter<HeartOfNatureConcentric>()
            .ActivateOnEnter<TasteOfBloodAndDuelOrDie>()
            .ActivateOnEnter<FlamesMeet>()
            .ActivateOnEnter<WindsPeak>()
            .ActivateOnEnter<WindsPeakKB>()
            .ActivateOnEnter<SplittingRage>()
            .ActivateOnEnter<NaturesBlood>()
            .ActivateOnEnter<MoveMountains>()
            .ActivateOnEnter<WildfireCrucible>();
    }
}

sealed class FlamesMeet : Components.SimpleAOEs
{
    public FlamesMeet(ModuleBase module) : base(module, (uint)AID.FlamesMeet, new AOEShapeCross(40f, 7f), 2)
    {
        MaxDangerColor = 1;
    }
}

[ModuleInfo(Group = ModuleGroup.BozjaDuel, GroupID = 778u, CFCID = 778u, NameID = 31u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "SourP (ported from BMR)")]
public sealed class Duel6Lyon(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(50f, -410f), new ArenaBoundsCircle(20f))
{
    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 20f);
}
