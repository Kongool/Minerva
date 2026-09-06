// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.MaskedCarnivale.Stage07.Act3;

public enum OID : uint
{
    Boss = 0x2706, //R=5.0
    Slime = 0x2707 //R=0.8
}

public enum AID : uint
{
    LowVoltage = 14710, // Boss->self, 12.0s cast, range 30+R circle - can be line of sighted by barricade
    Detonation = 14696, // Slime->self, no cast, range 6+R circle
    Object130 = 14711 // Boss->self, no cast, range 30+R circle - instant kill if you do not line of sight the towers when they die
}

sealed class LowVoltage(ModuleBase module) : Components.CastLineOfSightAOEComplex(module, (uint)AID.LowVoltage, Layouts.Layout2CornersBlockers, riskyWithSecondsLeft: 99d);

sealed class SlimeExplosion(ModuleBase module) : Components.GenericStackSpread(module)
{
    private readonly List<Actor> slimes = [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.Slime)
        {
            slimes.Add(actor);
        }
    }

    public override void OnActorDeath(Actor actor)
    {
        if (actor.OID == (uint)OID.Slime)
        {
            slimes.Remove(actor);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        var count = slimes.Count;
        for (var i = 0; i < count; ++i)
        {
            Arena.ZoneCircleOutline(slimes[i].Position, 7.6f);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var count = slimes.Count;
        for (var i = 0; i < count; ++i)
        {
            if (actor.Position.InCircle(slimes[i].Position, 7.6f))
            {
                hints.Add("In slime explosion radius!");
                return;
            }
        }
    }
}

sealed class Hints(ModuleBase module) : ModuleComponent(module)
{
    public override void AddGlobalHints(GlobalHints hints)
    {
        hints.Add("Pull or push the Lava Slimes to the towers and then hit the slimes\nfrom a distance to set off the explosions. The towers create a damage\npulse every 12s and a deadly explosion when they die. Take cover.");
    }
}

sealed class Stage07Act3States : StateMachineBuilder
{
    public Stage07Act3States(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<LowVoltage>()
            .Raw.Update = () => AllDeadOrDestroyed(Stage07Act3.Trash);
    }
}

[ModuleInfo(CFCID = 617u, NameID = 8095u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public sealed class Stage07Act3 : ModuleBase
{
    public Stage07Act3(WorldState ws, Actor primary) : base(ws, primary, Layouts.ArenaCenter, Layouts.Layout2Corners)
    {
        ActivateComponent<Hints>();
        ActivateComponent<SlimeExplosion>();
    }
    public static readonly uint[] Trash = [(uint)OID.Boss, (uint)OID.Slime];

    protected override bool CheckPull() => IsAnyActorInCombat(Trash);

    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actors(Enemies((uint)OID.Boss));
        Arena.Actors(Enemies((uint)OID.Slime));
    }
}
