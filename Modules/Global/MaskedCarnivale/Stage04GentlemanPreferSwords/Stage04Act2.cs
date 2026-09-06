// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage04.Act2;

public enum OID : uint
{
    Boss = 0x25D5, //R=2.5
    Beetle = 0x25D6 //R=0.6
}

public enum AID : uint
{
    AutoAttack1 = 6497, // Boss->player, no cast, single-target
    AutoAttack2 = 6499, // Beetle->player, no cast, single-target

    GrandStrike = 14366, // Boss->self, 1.5s cast, range 75+R width 2 rect
    MagitekField = 14369, // Boss->self, 5.0s cast, single-target
    Spoil = 14362, // Beetle->self, no cast, range 6+R circle
    MagitekRay = 14368 // Boss->location, 3.0s cast, range 6 circle
}

sealed class GrandStrike(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.GrandStrike, new AOEShapeRect(77.5f, 2f));
sealed class MagitekRay(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MagitekRay, 6f);
sealed class MagitekField(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.MagitekField);

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"{Module.PrimaryActor.Name} is weak to lightning spells.\nDuring the fight he will spawn 6 beetles.\nIf available use the Ram's Voice + Ultravibration combo for the instant kill.");
    }
}

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"{Module.PrimaryActor.Name} is weak against lightning spells and can be frozen.");
    }
}

sealed class Stage04Act2States : StateMachineBuilder
{
    public Stage04Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<MagitekField>()
            .ActivateOnEnter<MagitekRay>()
            .ActivateOnEnter<GrandStrike>()
            .ActivateOnEnter<Hints2>()
            .DeactivateOnEnter<Hints>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 614u, CFCID = 614u, NameID = 8087u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage04Act2 : ModuleBase
{
    public Stage04Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Beetle));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.Beetle => 1,
                _ => 0
            };
        }
    }
}
