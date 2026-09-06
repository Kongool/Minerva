// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage11.Act1;

public enum OID : uint
{
    Boss = 0x2718, //R=1.2
}

public enum AID : uint
{
    Fulmination = 14583, // Boss->self, 23.0s cast, range 60 circle
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("These bombs start self-destruction on combat start. Pull them together\nwith Sticky Tongue and attack them with anything to interrupt them.\nThey are weak against wind and strong against fire.");
    }
}

sealed class Stage11Act1States : StateMachineBuilder
{
    public Stage11Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .Raw.Update = () => AllDeadOrDestroyed((uint)OID.Boss);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 621u, CFCID = 621u, NameID = 2280u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage11Act1 : ModuleBase
{
    public Stage11Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }

    protected override bool CheckPull() => IsAnyActorInCombat((uint)OID.Boss);
}
