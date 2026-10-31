// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Global.CrucibleOfTheUnbroken.SecondBoard.WyvernPiece;

public enum OID : uint
{
    WyvernPiece = 0x4C58,
    WindSprite = 0x4C5B, // R1.600, x0 (spawn during fight)
    WhirlwindSmall = 0x4C59, // R2.000, x0 (spawn during fight), mixed types
    WhirlwindBig = 0x4C5A, // R3.000, x0 (spawn during fight)
    LiquidHellPuddle = 0x1EA66D, // R0.500, x0 (spawn during fight), EventObj type
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 49680, // WyvernPiece->player, no cast, single-target
    Teleport = 48173, // WyvernPiece->location, no cast, single-target
    TheStormsGrip = 48166, // WyvernPiece->self, 4.0s cast, range 60 circle
    Buffet = 48167, // WindSprite->self, 6.0s cast, range 40 width 10 rect
    Typhoon = 48168, // WyvernPiece->self, 3.0s cast, range 40 circle
    StrongWind = 48169, // WhirlwindSmall->player, no cast: a whirlwind's touch, drawn by Whirlwind (Korha, 2026-10-24)
    LiquidHellBoss = 48171, // WyvernPiece->self, 3.0s cast, single-target
    LiquidHell = 48172, // Helper->self, 6.0s cast, range 6 circle
    BlazingTrailBoss = 48174, // WyvernPiece->self, 7.0+1.0s cast, single-target
    BlazingTrail = 48175, // Helper->self, 8.0s cast, range 60 180.000-degree cone
    StormTrailBoss = 48176, // WyvernPiece->self, 5.0+1.0s cast, single-target
    StormTrailBoss1 = 48177, // WyvernPiece->self, 5.0+1.0s cast, single-target
    StormTrail = 48178, // Helper->self, 6.0s cast, range 25 60.000-degree cone
}

public enum SID : uint
{
    Burns = 3065, // none->player, extra=0x0
    Burns1 = 3066, // none->player, extra=0x0
}

//sealed class TheStormsGrip(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.TheStormsGrip); // TODO confirm this is not a raidwide
sealed class Buffet(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Buffet, new AOEShapeRect(40.0f, 5.0f));
sealed class Typhoon(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Typhoon, 10.0f); // TODO add AI
sealed class LiquidHell(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.LiquidHell, 6.0f);
// Behind the boss is the only ground Blazing Trail spares, and in a squeeze the way there runs round a wall of Liquid
// Hell pools: 24-36y, four to six seconds of walking, against a 7.7s cast. The dodge only starts once the cone is inside
// its horizon, a second in, and by then the way round may no longer fit: Korha, 2026-10-25 00:58, the north way round
// (35.6y) was planned and flipped to straight through two pools within half a second. A person goes at the teleport
// (2026-10-24 19:47, hand-steered, clean twice). So, for as long as the cast runs, stand behind the boss: a positioning
// instruction (forbidden for ever, without limit), which the dodge answers at once.
sealed class BlazingTrail(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BlazingTrail, new AOEShapeCone(60.0f, 90.0f.Degrees()))
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        foreach (ref readonly var aoe in this.ActiveAOEs(slot, actor))
            hints.AddForbiddenZone(new SDHalfPlane(aoe.Origin, aoe.Rotation.ToDirection()), DateTime.MaxValue);
    }
}
sealed class StormTrail(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.StormTrail, new AOEShapeCone(25.0f, 30.0f.Degrees()));
sealed class LiquidHellPuddle(ModuleBase module) : Components.Voidzone(module, 5.0f,
    module => module.Enemies((uint)OID.LiquidHellPuddle).Where(z => z.EventState != 7));

// A whirlwind waits about 3.5s at the edge it spawns on, then crosses the floor in a straight lane at 2.0 y/s (Rosa,
// 2026-10-25: 11.5, 7.5, 3.6, -0.6 ... every 2s, heading 0 or 180 only). Drawn only where it is, its lane was invisible:
// the dodge parked Rosa in one 4y ahead of it and walked into it (Strong Wind, 239 and Damage Down). So the lane ahead is
// avoided as well, a second of travel at a time and each timed for when the whirlwind gets there, a second early, so the
// dodge crosses a lane ahead of it but does not wait in one. The lane is for the dodge only: drawn, its four overlapping
// copies read as several whirlwinds for each one (the user, 2026-10-30), so the radar shows the whirlwind itself.
sealed class Whirlwind(ModuleBase module) : Components.GenericAOEs(module)
{
    private const float Speed = 2.0f;
    private const double Pause = 3.5d;
    private const int SecondsAhead = 4;
    private readonly List<AOEInstance> aoes = [];
    private readonly List<AOEInstance> lane = [];
    private readonly List<(Actor actor, WPos spawn, DateTime born)> puddles = [];

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID is (uint)OID.WhirlwindSmall or (uint)OID.WhirlwindBig)
        {
            puddles.Add((actor, actor.Position, World.CurrentTime));
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID is (uint)OID.WhirlwindSmall or (uint)OID.WhirlwindBig)
        {
            puddles.RemoveAll(p => p.actor == actor);
        }
    }

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        return CollectionsMarshal.AsSpan(aoes);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        foreach (var ahead in lane)
            hints.AddForbiddenZone(ahead);
    }

    public override void Update()
    {
        aoes.Clear();
        lane.Clear();
        var now = World.CurrentTime;
        foreach (var (puddle, spawn, born) in puddles)
        {
            // Its touch reaches past its 2y (3y) body: Strong Wind hit at 3.3y centre to centre (Saar, 2026-10-25 19:27, "judged
            // safe"), the player's own half yalm and the lag behind a 2 y/s mover. Drawn a yalm and a half wider (BossmodReborn's
            // 2.0/3.0 drew the body only).
            AOEShapeCapsule shape = puddle.OID == (uint)OID.WhirlwindSmall ? new AOEShapeCapsule(3.5f, 2.5f) : new AOEShapeCapsule(4.5f, 3.5f);
            aoes.Add(new(shape, puddle.Position, puddle.Rotation, color: Colors.Danger));
            var moving = (puddle.Position - spawn).LengthSq() > 0.09f;
            var start = moving ? now : born.AddSeconds(Pause) > now ? born.AddSeconds(Pause) : now;
            var dir = puddle.Rotation.ToDirection();
            for (var i = 1; i <= SecondsAhead; ++i)
            {
                lane.Add(new(shape, puddle.Position + dir * (Speed * i), puddle.Rotation, start.AddSeconds(i - 1)));
            }
        }
    }
}

sealed class WyvernPieceStates : StateMachineBuilder
{
    public WyvernPieceStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            //.ActivateOnEnter<TheStormsGrip>()
            .ActivateOnEnter<Buffet>()
            .ActivateOnEnter<Typhoon>()
            .ActivateOnEnter<LiquidHell>()
            .ActivateOnEnter<Whirlwind>()
            .ActivateOnEnter<LiquidHellPuddle>()
            .ActivateOnEnter<BlazingTrail>()
            .ActivateOnEnter<StormTrail>();
    }
}

[ModuleInfo(CFCID = 1089u, NameID = 14549u, PrimaryActorOID = (uint)OID.WyvernPiece, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
public sealed class WyvernPiece(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(520f, 0f), new ArenaBoundsRect(20f, 14.8f));
