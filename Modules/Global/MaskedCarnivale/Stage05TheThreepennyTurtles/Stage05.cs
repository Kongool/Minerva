// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage05;

public enum OID : uint
{
    Boss = 0x25CC, //R=5.0
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("These turtles have very high defenses.\nBring 1000 Needles or Doom to defeat them.\nAlternatively you can remove their buff with Eerie Soundwave.");
    }
}

sealed class Stage05States : StateMachineBuilder
{
    public Stage05States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .Raw.Update = () => AllDeadOrDestroyed((uint)OID.Boss);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 615u, CFCID = 615u, NameID = 8089u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage05 : ModuleBase
{
    public Stage05(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleSmall)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }

    protected override bool CheckPull() => IsAnyActorInCombat((uint)OID.Boss);
}
