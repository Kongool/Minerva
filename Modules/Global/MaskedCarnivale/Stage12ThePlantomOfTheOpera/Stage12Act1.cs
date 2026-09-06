// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage12.Act1;

public enum OID : uint
{
    Boss = 0x271A //R=0.8
}

public enum AID : uint
{
    Seedvolley = 14750 // Boss->player, no cast, single-target
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("For this stage Ice Spikes and Bomb Toss are recommended spells.\nUse Ice Spikes to instantly kill roselets once they become aggressive.\nHydnora in act 2 is weak against water and strong against earth spells.");
    }
}

sealed class Stage12Act1States : StateMachineBuilder
{
    public Stage12Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 622u, CFCID = 622u, NameID = 8103u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage12Act1 : ModuleBase
{
    public Stage12Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }
}
