// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage11.Act2;

public enum OID : uint
{
    Boss = 0x2719 //R=1.2
}

public enum AID : uint
{
    Fulmination = 14583 // 2719->self, 23.0s cast, range 60 circle
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Same as last act, but this time there are 4 bombs. Pull them to the\nmiddle with Sticky Tongue and attack them with any AOE to keep them\ninterrupted. They are weak against wind and strong against fire.");
    }
}

sealed class Stage11Act2States : StateMachineBuilder
{
    public Stage11Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .Raw.Update = () => AllDeadOrDestroyed((uint)OID.Boss);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 621u, CFCID = 621u, NameID = 2280u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage11Act2 : ModuleBase
{
    public Stage11Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.Layout4Quads)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }

    protected override bool CheckPull() => IsAnyActorInCombat((uint)OID.Boss);
}
