// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.Duel.Duel2Lyon;

sealed class Duel2LyonStates : StateMachineBuilder
{
    public Duel2LyonStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<Enaero>()
            .ActivateOnEnter<HeartOfNature>()
            .ActivateOnEnter<TasteOfBlood>()
            .ActivateOnEnter<TasteOfBloodHint>()
            .ActivateOnEnter<RavenousGale>()
            .ActivateOnEnter<WindsPeakKB>()
            .ActivateOnEnter<WindsPeak>()
            .ActivateOnEnter<SplittingRage>()
            .ActivateOnEnter<TheKingsNotice>()
            .ActivateOnEnter<TwinAgonies>()
            .ActivateOnEnter<NaturesBlood>()
            .ActivateOnEnter<SpitefulFlameCircleVoidzone>()
            .ActivateOnEnter<SpitefulFlameRect>()
            .ActivateOnEnter<DynasticFlame>()
            .ActivateOnEnter<SkyrendingStrike>();
    }
}

[ModuleInfo(Group = ModuleGroup.BozjaDuel, CFCID = 735u, NameID = 8u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")] // bnpcname=9409
public sealed class Duel2Lyon(WorldState ws, Actor primary) : ModuleBase(ws, primary, startingArena.Center, startingArena)
{
    private static readonly ArenaBoundsCustom startingArena = new([new Polygon(new(211f, 380f), 24.5f, 32)]);
    public static readonly ArenaBoundsCircle DefaultArena = new(20f); // default arena got no extra collision, just a donut aoe

    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 25f);
}
