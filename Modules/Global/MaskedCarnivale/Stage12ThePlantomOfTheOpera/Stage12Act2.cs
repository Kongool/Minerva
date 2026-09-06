// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage12.Act2;

public enum OID : uint
{
    Boss = 0x271B, //R=6.96
    Roselet = 0x271C //R=0.8
}

public enum AID : uint
{
    WildHorn = 14751, // Boss->self, 3.5s cast, range 10+R 120-degree cone
    SporeSac = 14752, // Boss->self, 3.0s cast, range 50 circle
    Seedvolley = 14750, // Roselet->player, no cast, single-target
    Trounce = 14754, // Boss->self, 4.5s cast, range 40+R 60-degree cone
    InflammableFumes = 14753 // Boss->self, 15.0s cast, range 50 circle
}

sealed class WildHorn(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.WildHorn, new AOEShapeCone(16.96f, 60f.Degrees()));
sealed class Trounce(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Trounce, new AOEShapeCone(46.96f, 30f.Degrees()));
sealed class SporeSac(ModuleBase module) : Components.CastHint(module, (uint)AID.SporeSac, "Calls Roselets. Prepare Ice Spikes if available.");
sealed class InflammableFumes(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.InflammableFumes, false, true);

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add($"Use Bomb Toss to stun {Module.PrimaryActor.Name} when he casts Inflammable Fumes.\nUse Ice Spikes to instantly kill roselets once they become aggressive.\n{Module.PrimaryActor.Name} is weak against water and strong against earth spells.");
    }
}

sealed class Stage12Act2States : StateMachineBuilder
{
    public Stage12Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<WildHorn>()
            .ActivateOnEnter<Trounce>()
            .ActivateOnEnter<SporeSac>()
            .ActivateOnEnter<InflammableFumes>()
            .DeactivateOnEnter<Hints>();
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 622u, CFCID = 622u, NameID = 8102u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage12Act2 : ModuleBase
{
    public Stage12Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
    }

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Roselet), Colors.Object);
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.Roselet => 1, // TODO: ideally AI would use Ice Spikes when these spawn instead of attacking them directly
                _ => 0
            };
        }
    }
}
