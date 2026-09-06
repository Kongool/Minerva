// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage08.Act2;

public enum OID : uint
{
    Boss = 0x270B, //R=3.75
    Bomb = 0x270C, //R=0.6
    Snoll = 0x270D //R=0.9
}

public enum AID : uint
{
    AutoAttack = 6499, // Bomb/Boss->player, no cast, single-target

    SelfDestruct = 14730, // Bomb->self, no cast, range 6 circle
    HypothermalCombustion = 14731, // Snoll->self, no cast, range 6 circle
    Sap = 14708, // Boss->location, 5.0s cast, range 8 circle
    Burst = 14680 // Boss->self, 6.0s cast, range 50 circle
}

sealed class Sap(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Sap, 8f);
sealed class Burst(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.Burst);

sealed class Selfdetonations(ModuleBase module) : ModuleComponent(module)
{
    private const string hint = "In bomb explosion radius!";

    private readonly List<Actor> bombs = [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID is (uint)OID.Bomb or (uint)OID.Snoll)
        {
            bombs.Add(actor);
        }
    }

    public override void OnActorDeath(Actor actor)
    {
        if (actor.OID is (uint)OID.Bomb or (uint)OID.Snoll)
        {
            bombs.Remove(actor);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var count = bombs.Count;
        for (var i = 0; i < count; ++i)
        {
            Arena.ZoneCircleOutline(bombs[i].Position, 6f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var count = bombs.Count;
        for (var i = 0; i < count; ++i)
        {
            if (actor.Position.InCircle(bombs[i].Position, 6f))
            {
                hints.Add(hint);
                return;
            }
        }
    }
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Clever activation of cherry bombs will freeze the Progenitrix.\nInterrupt its burst skill or wipe. The Progenitrix is weak to wind spells.");
    }
}

sealed class Stage08Act2States : StateMachineBuilder
{
    public Stage08Act2States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Burst>()
            .ActivateOnEnter<Sap>()
            .Raw.Update = () => AllDeadOrDestroyed(Stage08Act2.Trash);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 618u, CFCID = 618u, NameID = 8098u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage08Act2 : ModuleBase
{
    public Stage08Act2(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.Layout2Corners)
    {
        ActivateComponent<Hints>();
        ActivateComponent<Selfdetonations>();
    }
    public static readonly uint[] Trash = [(uint)OID.Boss, (uint)OID.Bomb, (uint)OID.Snoll];

    protected override bool CheckPull() => IsAnyActorInCombat(Trash);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.Bomb));
        Arena.Actors(Enemies((uint)OID.Snoll));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.Boss => 1,
                _ => 0
            };
        }
    }
}
