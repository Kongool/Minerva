// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage08.Act1;

public enum OID : uint
{
    Boss = 0x2708, //R=0.6
    Bomb = 0x2709, //R=1.2
    Snoll = 0x270A //R=0.9
}

public enum AID : uint
{
    SelfDestruct = 14687, // Boss->self, no cast, range 10 circle
    HypothermalCombustion = 14689, // Snoll->self, no cast, range 6 circle
    SelfDestruct2 = 14688 // Bomb->self, no cast, range 6 circle
}

sealed class Selfdetonation(ModuleBase module) : ModuleComponent(module)
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
        if (!Module.PrimaryActor.IsDead)
        {
            Arena.ZoneCircleOutline(Module.PrimaryActor.Position, 10f);
        }
        var count = bombs.Count;
        for (var i = 0; i < count; ++i)
        {
            Arena.ZoneCircleOutline(bombs[i].Position, 6f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (!Module.PrimaryActor.IsDead && actor.Position.InCircle(Module.PrimaryActor.Position, 10f))
        {
            hints.Add(hint);
            return;
        }
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
        hints.Add("For this stage the spell Flying Sardine to interrupt the Progenitrix in Act 2\nis highly recommended. Hit the Cherry Bomb from a safe distance\nwith anything but fire damage to set of a chain reaction to win this act.");
    }
}

sealed class Hints2(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Hit the Cherry Bomb from a safe distance to win this act.");
    }
}

sealed class Stage08Act1States : StateMachineBuilder
{
    public Stage08Act1States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .DeactivateOnEnter<Hints>()
            .ActivateOnEnter<Hints2>()
            .Raw.Update = () => AllDeadOrDestroyed(Stage08Act1.Trash);
    }
}

[ModuleInfo(Group = ModuleGroup.MaskedCarnivale, GroupID = 618u, CFCID = 618u, NameID = 8140u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage08Act1 : ModuleBase
{
    public Stage08Act1(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.CircleBig)
    {
        ActivateComponent<Hints>();
        ActivateComponent<Selfdetonation>();
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
