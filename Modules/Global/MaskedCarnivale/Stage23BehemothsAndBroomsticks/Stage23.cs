// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage23;

public enum OID : uint
{
    Boss = 0x2732, //R=5.8
    Maelstrom = 0x2733, //R=1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target
    Charybdis = 15258, // Boss->location, 3.0s cast, range 6 circle
    Maelstrom = 15259, // Maelstrom->self, 1.0s cast, range 8 circle, pull dist 40 into center, vuln stack
    Trounce = 15256, // Boss->self, 3.5s cast, range 50+R 60-degree cone
    ComeVisual = 15260, // Boss->self, 5.0s cast, single-target
    Comet = 15261, // Helper->location, 4.0s cast, range 10 circle
    EclipticMeteor = 15257 // Boss->location, 10.0s cast, range 50 circle
}

sealed class Charybdis(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Charybdis, 6f);
sealed class Maelstrom(ModuleBase module) : Components.Voidzone(module, 8f, GetMaelstrom)
{
    private static List<Actor> GetMaelstrom(ModuleBase module) => module.Enemies((uint)OID.Maelstrom);
}
sealed class Trounce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Trounce, new AOEShapeCone(55.8f, 30f.Degrees()));
sealed class Comet(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Comet, 10f);
sealed class EclipticMeteor(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.EclipticMeteor, "Use Diamondback!");

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"The {Module.PrimaryActor.Name} will use Ecliptic Meteor.\nUse Diamondback to survive it.\nYou can start the Final Sting combination at about 40% health left.\n(Off-guard->Bristle->Moonflute->Final Sting)");
    }
}

sealed class Stage23States : StateMachineBuilder
{
    public Stage23States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Charybdis>()
            .ActivateOnEnter<Maelstrom>()
            .ActivateOnEnter<Trounce>()
            .ActivateOnEnter<Comet>()
            .ActivateOnEnter<EclipticMeteor>()
            .DeactivateOnEnter<Hints>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 633u, CFCID = 633u, NameID = 8124u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage23 : ModuleBase
{
    public Stage23(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleSmall)
    {
        ActivateComponent<Hints>();
    }
}
