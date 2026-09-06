// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Unreal.Un1Ultima;

// TODO: consider how phase changes could be detected and create different states for them?..
class Phases(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        var hint = ((float)Module.PrimaryActor.HPMP.CurHP / Module.PrimaryActor.HPMP.MaxHP) switch
        {
            > 0.8f => "Garuda -> 80% Titan",
            > 0.65f => "Titan -> 65% Ifrit",
            > 0.5f => "Ifrit -> 50% Boom1",
            > 0.4f => "Boom1 -> 40% Bits1",
            > 0.3f => "Bits1 -> 30% Boom2",
            > 0.25f => "Boom2 -> 25% Bits2",
            > 0.15f => "Bits2 -> 15% Boom3",
            _ => "Boom3 -> Enrage"
        };
        hints.Add(hint);
    }
}

public class Un1UltimaStates : StateMachineBuilder
{
    public Un1UltimaStates(ModuleBase module) : base(module)
    {
        // TODO: reconsider
        TrivialPhase(0, 600)
            .ActivateOnEnter<Phases>()
            .ActivateOnEnter<Mechanics>()
            .ActivateOnEnter<Garuda>()
            .ActivateOnEnter<TitanIfrit>();
    }
}

[ModuleInfo(CFCID = 825u, NameID = 2137u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")]
public class Un1Ultima(WorldState ws, Actor primary) : ModuleBase(ws, primary, default, new ArenaBoundsCircle(20));
