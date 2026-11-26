// BossmodReborn's components for this fight that Minerva's own module (V3Quaqua.cs, written from recordings before
// BossmodReborn had one) has nothing for: route mechanics no recording has: Arcane Intervention's gaze, Arcane Pursuit's laser and orbs, the poison puddles. Activated from that module's states. Everything
// else in BossmodReborn's file covers actions the hand-written module already handles, and on the recorded routes the
// hand-written handling does better in the closed loop (review/C8.md), so it was left out. Ported 2026-11-26 from
// BossmodReborn 1aa851810; not seen in a recording unless the hand-written module's notes say so.
// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V03Aloalo.V031Quaqua;

public enum OID : uint
{
    Boss = 0x40BA, // R5.250, x1
    Helper = 0x233C, // R0.500, x?, Helper type
    AethericCharge = 0x40BB, // R1.500-2.200, x?, sphere for Arcane Armaments
    SpearMarker = 0x40BD, // R0.500, x5, middle-path water spear tether source
    HammerBeacon = 0x40E0, // R1.000, x?, Hammer Landing jump markers
    DrakeFamiliarLarge = 0x40BF, // R2.700, x?, Cloud to Ground visual (stormy)
    DrakeFamiliar = 0x4135, // R1.000, x?, Cloud to Ground exaflares (stormy)
    AnalaFamiliar = 0x4134, // R1.000, x?, Scalding Waves (fair skies)
    PoisonPuddle = 0x1EB94A, // R0.500, EventObj type, growing poison pools (4+ flowers path)
    PursuitCharge = 0x40BC, // R1.500, x?, chasing orb for Arcane Pursuit (both statues repaired)
    AnalaFamiliarLarge = 0x40BE, // R1.500, x0 (spawn during fight), initial Scalding Waves casters (fair skies)
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target
    Teleport = 35727, // Boss->location, no cast, single-target

    MadeMagic = 35732, // Boss->self, 5.0s cast, range 50 circle, raidwide + spawn spheres
    ArcaneArmamentsAxeQuoit = 35720, // Boss->self, 8.0s cast, single-target, tether spheres (axe/chakram)
    ArcaneArmamentsResolve = 35721, // Helper->self, no cast, single-target, visual when icon reaches sphere
    RavagingAxe = 35722, // AethericCharge->self, 2.0s cast, range 14 circle
    RingingQuoits = 35723, // AethericCharge->self, 2.0s cast, range 5-18 donut

    ArcaneArmamentsSpearHammer = 35728, // Boss->self, 5.0s cast, single-target, left-path spears (Rout) / hammers
    Rout = 35729, // Boss->location, 5.0s cast, range 45 width 16 rect
    RoutRepeat = 35730, // Boss->location, no cast, range 45 width 16 rect

    ArcaneArmamentsHammers = 35724, // Boss->self, 5.0s cast, single-target, spawn hammer beacons
    HammerLanding = 35725, // Boss->location, 8.0s cast, range 40 circle, knockback
    HammerLandingRepeat = 35726, // Boss->location, no cast, range 40 circle, knockback

    // Middle path
    ArcaneArmamentsWaterSpears = 35743, // Boss->self, 8.0s cast, single-target, tether spear markers
    ElementalImpact = 35744, // Helper->self, 6.5s cast, range 14 circle
    FlowingLanceCW = 35745, // Helper->self, 8.0s cast, range 24 width 12 cross (turns right / CW)
    FlowingLanceCCW = 36049, // Helper->self, 8.0s cast, range 24 width 12 cross (turns left / CCW)
    FlowingLanceRest = 35746, // Helper->self, 1.0s cast, range 24 width 12 cross

    VioletStorm = 35733, // Boss->self, 5.5s cast, range 32 120-degree cone
    Howl = 35734, // Boss->self, 4.0s cast, single-target, summon familiars (left path)

    // Fair skies - analas
    ScaldingWaves = 35731, // Helper->self, 2.7-3.0s cast, range 40 width 10 rect
    ScaldingWavesFairSkies = 35735, // AnalaFamiliarLarge->self, 5.0s cast, range 50 width 8 rect
    ScaldingWavesFairSkiesRepeat = 35736, // AnalaFamiliar->self, no cast, range 50 width 4 rect

    // Stormy - drakes
    CloudToGround = 35739, // DrakeFamiliarLarge->self, 4.0s cast, single-target, visual
    CloudToGroundFirst = 35740, // DrakeFamiliar->self, 5.0s cast, range 6 circle
    CloudToGroundRest = 35741, // DrakeFamiliar->self, 1.0s cast, range 6 circle

    ArcaneArmamentsPoisonSpears = 35747, // Boss->self, 8.0s cast, single-target

    ArcaneIntervention = 35758, // Helper->self, 5.0s cast, range 50 circle, gaze

    ArcanePursuit = 35748, // Boss->self, 5.0s cast, single-target, spawn chasing orbs
    FellForces = 35749, // PursuitCharge->player, no cast, single-target, contact damage (range 1)
    ArcanePursuitLaser = 35750, // Helper->self, 5.0s cast, range 50 width 6 rect (destroys orbs, harmless to players)
    ArcanePursuitLaserSecond = 35757, // Helper->self, 5.0s cast, range 50 width 6 rect
}

public enum TetherID : uint
{
    Axe = 256, // Ravaging Axe (circle)
    Quoit = 257, // Ringing Quoits (donut)
    Hammer = 249, // HammerBeacon->Boss
    WaterSpear = 258, // SpearMarker->Boss
}

public enum IconID : uint
{
    TurnRight = 235, // Helper->self, Flowing Lance CW (-15 degrees)
    TurnLeft = 236, // Helper->self, Flowing Lance CCW (+15 degrees)
}

sealed class ArcaneIntervention(ModuleBase module) : Components.CastGaze(module, (uint)AID.ArcaneIntervention);

sealed class ArcanePursuit(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> _lasers = [];
    private static readonly AOEShapeRect _laser = new(50, 3);
    private const float KeepAway = 3.5f;

    private readonly List<Actor> _charges = [];

    // note: returns a shared buffer, which is overwritten by the next access
    private List<Actor> Charges
    {
        get
        {
            _charges.Clear();
            foreach (var c in Module.Enemies((uint)OID.PursuitCharge))
                if (!c.IsDead)
                    _charges.Add(c);
            return _charges;
        }
    }

    private readonly List<AOEInstance> _aoesBuffer = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        _aoesBuffer.Clear();
        foreach (var c in Charges)
            _aoesBuffer.Add(new(new AOEShapeCircle(KeepAway), c.Position));
        // safe zones
        foreach (var l in _lasers)
            if (l.CastInfo != null)
                _aoesBuffer.Add(new(_laser, l.Position, l.CastInfo.Rotation, Module.CastFinishAt(l.CastInfo), Colors.SafeFromAOE, risky: false));
        return CollectionsMarshal.AsSpan(_aoesBuffer);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        var charges = Charges;
        if (charges.InRadius(actor.Position, KeepAway).Any())
            hints.Add("Away from orb!");
        else if (_lasers.Count > 0 && charges.Count != 0)
            hints.Add("Lead orb into statue laser!", false);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var c in Charges)
        {
            hints.AddForbiddenZone(new SDCircle(c.Position, KeepAway));
            hints.AddForbiddenZone(new SDCapsule(c.Position, c.Rotation.ToDirection(), 8, KeepAway), World.FutureTime(2));
        }

        foreach (var l in _lasers)
        {
            if (l.CastInfo == null)
                continue;
            var rot = l.CastInfo.Rotation;
            var origin = l.Position;
            var finish = Module.CastFinishAt(l.CastInfo);
            hints.AddForbiddenZone(new SDInvertedRect(origin, rot, 50, 0, 3), finish);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var c in Charges)
        {
            var target = World.Actors.Find(c.TargetID);
            if (target != null)
            {
                Arena.AddLine(c.Position, target.Position, target == pc ? Colors.Danger : Colors.PlayerGeneric);
            }
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.ArcanePursuitLaser or (uint)AID.ArcanePursuitLaserSecond)
            _lasers.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.ArcanePursuitLaser or (uint)AID.ArcanePursuitLaserSecond)
            _lasers.Remove(caster);
    }
}

sealed class ArcaneArmamentsPoison(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<(Actor Actor, DateTime Spawn)> _puddles = [];
    private bool _armamentsActive;

    private const float PuddleStartRadius = 6;
    private const float PuddleMaxRadius = 16;
    private const float GrowSeconds = 14;

    private readonly List<AOEInstance> _aoesBuffer = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        _aoesBuffer.Clear();
        foreach (var (puddle, spawn) in _puddles)
        {
            var t = (float)(World.CurrentTime - spawn).TotalSeconds;
            var radius = Math.Clamp(PuddleStartRadius + (PuddleMaxRadius - PuddleStartRadius) * (t / GrowSeconds), PuddleStartRadius, PuddleMaxRadius);
            _aoesBuffer.Add(new(new AOEShapeCircle(radius), puddle.Position, default, spawn));
            if (radius < PuddleMaxRadius - 0.5f)
                _aoesBuffer.Add(new(new AOEShapeCircle(PuddleMaxRadius), puddle.Position, default, spawn.AddSeconds(GrowSeconds), Colors.AOE, risky: false));
        }
        return CollectionsMarshal.AsSpan(_aoesBuffer);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ArcaneArmamentsPoisonSpears)
        {
            _armamentsActive = true;
            _puddles.Clear();
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ArcaneArmamentsPoisonSpears)
            _armamentsActive = true; // puddles still inbound briefly after cast
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.PoisonPuddle)
        {
            _puddles.Add((actor, World.CurrentTime));
            _armamentsActive = false;
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        _puddles.RemoveAll(p => p.Actor == actor);
    }

    public override void OnActorEState(Actor actor, ushort state)
    {
        if (actor.OID == (uint)OID.PoisonPuddle)
            _puddles.RemoveAll(p => p.Actor == actor);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);

        var origins = _originsBuffer;
        origins.Clear();
        if (_puddles.Count > 0)
        {
            foreach (var p in _puddles)
                origins.Add(p.Actor.Position);
        }
        else if (_armamentsActive)
        {
            foreach (var s in Module.Enemies((uint)OID.SpearMarker))
                if (!s.IsDead && s.Tether.ID == (uint)TetherID.WaterSpear)
                    origins.Add(s.Position);
        }
        if (origins.Count < 2)
            return;

        // midpoint between furthest spears
        WPos a = default, b = default;
        var best = 0f;
        for (var i = 0; i < origins.Count; ++i)
        {
            for (var j = i + 1; j < origins.Count; ++j)
            {
                var d = (origins[i] - origins[j]).LengthSq();
                if (d > best)
                {
                    best = d;
                    a = origins[i];
                    b = origins[j];
                }
            }
        }

        var mid = a + (b - a) * 0.5f;
        var toWall = mid - Module.Center;
        if (toWall.LengthSq() < 0.01f)
            return;
        var safe = Module.Center + toWall.Normalized() * (Module.Bounds.Radius - 1.5f);
        hints.GoalZones.Add(AIHints.GoalProximity(safe, 3, 40));
    }

    private readonly List<WPos> _originsBuffer = [];
}

