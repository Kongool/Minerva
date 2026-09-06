// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage16.Act1;

public enum OID : uint
{
    Boss = 0x26F2 //R=3.2
}

public enum AID : uint
{
    AutoAttack = 6497 // Boss->player, no cast, single-target
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("The cyclops are very slow, but will instantly kill you, if they catch you.\nKite them or kill them with the self-destruct combo. (Toad Oil->Bristle->\nMoonflute->Swiftcast->Self-destruct) If you don't use the self-destruct\ncombo in act 1, you can bring the Final Sting combo for act 2.\n(Off-guard->Bristle->Moonflute->Final Sting)\nDiamondback is highly recommended in act 2.");
    }
}

sealed class Stage16Act1States : StateMachineBuilder
{
    public Stage16Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .Raw.Update = () => AllDeadOrDestroyed((uint)OID.Boss);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 626u, CFCID = 626u, NameID = 8112u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage16Act1 : ModuleBase
{
    public Stage16Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }

    protected override bool CheckPull() => IsAnyActorInCombat((uint)OID.Boss);
}
