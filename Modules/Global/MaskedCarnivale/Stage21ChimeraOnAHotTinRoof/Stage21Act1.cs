// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage21.Act1;

public enum OID : uint
{
    Boss = 0x272F //R=0.45
}

public enum AID : uint
{
    Blizzard = 14267, // Boss->player, 1.0s cast, single-target
    VoidBlizzard = 15063, // Boss->player, 6.0s cast, single-target
    Icefall = 15064 // Boss->location, 2.5s cast, range 5 circle
}

sealed class Icefall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Icefall, 5f);
sealed class VoidBlizzard(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.VoidBlizzard);

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("The first act is fairly easy. Interrupt the Void Blizzards with Flying\nSardine and most of the danger is gone. The Imps are weak against fire spells.\nIn the 2nd act you can start the Final Sting combination at about 50%\nhealth left. (Off-guard->Bristle->Moonflute->Final Sting)");
    }
}

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("The imps are weak to fire spells and strong against ice.\nInterrupt Void Blizzard with Flying Sardine.");
    }
}

sealed class Stage21Act1States : StateMachineBuilder
{
    public Stage21Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<VoidBlizzard>()
            .ActivateOnEnter<Icefall>()
            .ActivateOnEnter<Hints2>()
            .DeactivateOnEnter<Hints>()
            .Raw.Update = () => AllDeadOrDestroyed((uint)OID.Boss);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 631u, CFCID = 631u, NameID = 8120u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage21Act1 : ModuleBase
{
    public Stage21Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
    }

    protected override bool CheckPull() => IsAnyActorInCombat((uint)OID.Boss);
}
