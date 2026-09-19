// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Foray.CriticalEngagement.CE13KillItWithFire;

public enum OID : uint
{
    Boss = 0x2E2F, // R2.25
    RottenMandragora = 0x2E30, // R1.05
    Pheromones = 0x2E31, // R1.5
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6499, // Boss->player, no cast, single-target
    Teleport = 20513, // Boss->location, no cast, single-target, teleport

    HarvestFestival = 20511, // Boss->self, 4.0s cast, single-target, visual (summon mandragoras)
    PheromonesVisual1 = 20512, // RottenMandragora->self, no cast, single-target, visual (???)
    PheromonesVisual2 = 20514, // RottenMandragora->location, no cast, single-target, visual (???)
    RancidPheromones = 20515, // RottenMandragora->self, no cast, single-target, visual (???)
    Heartbreak = 20516, // Pheromones->self, no cast, range 4 circle when pheromone is touched
    DeadLeaves = 20517, // Boss->self, 4.0s cast, range 30 circle, visual (recolors)
    TenderAnaphylaxis = 20518, // Helper->self, 4.0s cast, range 30 90-degree cone
    JealousAnaphylaxis = 20519, // Helper->self, 4.0s cast, range 30 90-degree cone
    AnaphylacticShock = 20520, // Helper->self, 4.0s cast, range 30 width 2 rect aoe (borders)
    SplashBomb = 20521, // Boss->self, 4.0s cast, single-target, visual (puddles)
    SplashBombAOE = 20522, // Helper->self, 4.0s cast, range 6 circle puddle
    SplashGrenade = 20523, // Boss->self, 5.0s cast, single-target, visual (stack)
    SplashGrenadeAOE = 20524, // Helper->players, 5.0s cast, range 6 circle stack
    PlayfulBreeze = 20525, // Boss->self, 4.0s cast, single-target, visual (raidwide)
    PlayfulBreezeAOE = 20526, // Helper->self, 4.0s cast, range 60 circle raidwide
    Budbutt = 20527 // Boss->player, 4.0s cast, single-target, tankbuster
}

public enum SID : uint
{
    TenderAnaphylaxis = 2301, // Helper->player, extra=0x0
    JealousAnaphylaxis = 2302 // Helper->player, extra=0x0
}

sealed class Pheromones(ModuleBase module) : Components.Voidzone(module, 4f, GetVoidzones, 3f)
{
    private static List<Actor> GetVoidzones(ModuleBase module) => module.Enemies((uint)OID.Pheromones);
}

// Tender and Jealous Anaphylaxis: four cones on a four-second cast, two of each colour. Two corrections, both
// measured on the 2026-09-17 Peerifool recording.
//
// The width. Players were damaged up to 27 degrees off a cone's centre and spared from 31 degrees out: 60 degrees
// wide, not the 90 the port drew. At 90 the four of them cover the arena and the dodge reports no safe spot at all.
//
// The colours. The port showed a player only its own colour's cones, keyed on the Tender/Jealous status. That status
// is applied BY the hit and runs about nine seconds -- which expires on the very tick the next round lands. So at the
// moment of damage nobody holds anything: 31 of 42 hits were on players holding no status, and the character here was
// hit by a Tender cone while it had held Jealous through the whole cast. The colour decides nothing about who is
// caught, so every cone is drawn for everybody.
sealed class DeadLeaves(ModuleBase module) : Components.SimpleAOEGroups(module, [(uint)AID.TenderAnaphylaxis, (uint)AID.JealousAnaphylaxis], new AOEShapeCone(30f, 30f.Degrees()));

sealed class AnaphylacticShock(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AnaphylacticShock, new AOEShapeRect(30f, 1f));
sealed class SplashBomb(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.SplashBombAOE, 6f);
sealed class SplashGrenade(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.SplashGrenadeAOE, 6f, 8);
sealed class PlayfulBreeze(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.PlayfulBreeze);
sealed class Budbutt(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Budbutt);

sealed class CE13KillItWithFireStates : StateMachineBuilder
{
    public CE13KillItWithFireStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Pheromones>()
            .ActivateOnEnter<DeadLeaves>()
            .ActivateOnEnter<AnaphylacticShock>()
            .ActivateOnEnter<SplashBomb>()
            .ActivateOnEnter<SplashGrenade>()
            .ActivateOnEnter<PlayfulBreeze>()
            .ActivateOnEnter<Budbutt>();
    }
}

[ModuleInfo(Group = ModuleGroup.CriticalEngagement, CFCID = 735u, NameID = 1u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "ported from BossmodReborn")] // bnpcname=9391
public sealed class CE13KillItWithFire(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-90f, 700f), 29.5f, 32)]);

    protected override bool CheckPull() => base.CheckPull() && Raid.Player()!.Position.InCircle(Center, 30f);
}
