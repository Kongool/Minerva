// BossmodReborn's components for this fight that Minerva's own module (V3Ketuduke.cs, written from recordings before
// BossmodReborn had one) has nothing for: Hydroblast, Water III, Tidal Wave's and Fluke Typhoon's knockbacks, Hydrosurge. Activated from that module's states. Everything
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

namespace Minerva.Endwalker.VariantCriterion.V03Aloalo.V032Ketuduke;

public enum OID : uint
{
    Boss = 0x4091, // R8.000, x1
    Helper = 0x233C, // R0.500, x?, Helper type
    KnockbackHelper = 0x40B2, // R0.500, x?
    SpringCrystalSphere = 0x4092, // R4.200, x?, polyhedron crystals (point-blank Saturate)
    SpringCrystalRect = 0x4093, // R4.200, x?, flat crystals (line Saturate)
    AiryBubble = 0x4095, // R1.300, x?, Blowing Bubbles
    ZealBlindZozone = 0x4096, // R0.500, x?, route 1/2 NPC
    SummonedApa = 0x4113, // R2.880, x?, Water III / Wavefoam
    SummonedAnila = 0x4097, // R2.400, x?, route 2 Fluke Typhoon
    WavefoamBubble = 0x4094, // R1.200, x?, route 2 stand-in bubbles
    BubbleStrewer = 0x1EB936, // R0.500, EventObj type, Strewn Bubbles lines
    AloaloZaratan = 0x409A, // R1.120, x?, route 3 sand pile snakes
    AloaloOgrebon = 0x4098, // R2.340, x?, route 4 stone pile fish
}

public enum AID : uint
{
    AutoAttack = 35448, // Boss->player, no cast, single-target
    Teleport = 35447, // Boss->location, no cast, single-target

    TidalRoar = 35493, // Boss->self, 5.0s cast, single-target, visual
    TidalRoarAOE = 35494, // Helper->self, no cast, range 100 circle, raidwide

    SpringCrystals = 35449, // Boss->self, 2.2+0.8s cast, single-target, visual
    SpringCrystalSphereAppear = 35450, // SpringCrystalSphere->self, no cast, single-target, visual
    SaturateSphere = 35452, // SpringCrystalSphere->self, 3.0s cast, range 8 circle
    SaturateSphereFast = 35453, // SpringCrystalSphere->self, 1.0s cast, range 8 circle
    SpringCrystalRectAppear = 35451, // SpringCrystalRect->self, no cast, single-target, visual
    SaturateRect = 35454, // SpringCrystalRect->self, 3.0s cast, range 76 width 10 rect
    SaturateRectFast = 35455, // SpringCrystalRect->self, 1.0s cast, range 76 width 10 rect

    BubbleNet = 35456, // Boss->self, 4.1+0.9s cast, single-target, visual
    BubbleNetAOE = 35457, // Helper->self, 5.0s cast, range 65 circle, raidwide

    FlukeTyphoon = 35460, // Boss->self, 3.0s cast, single-target, visual (move bubbled crystal)
    FlukeTyphoonAOE = 35461, // Helper->self, 5.0s cast, range 40 width 40 rect

    StrewnBubbles = 35462, // Boss->self, 2.2+0.8s cast, single-target, visual
    SphereShatter = 35463, // Helper->self, no cast, range 20 width 10 rect

    RecedingTwintides = 35485, // Boss->self, 5.0s cast, range 14 circle
    FarTide = 35488, // Boss->self, 1.5s cast, range 8-60 donut
    EncroachingTwintides = 35487, // Boss->self, 5.0s cast, range 8-60 donut
    NearTide = 35486, // Boss->self, 1.5s cast, range 14 circle

    Hydrobomb = 35489, // Boss->self, 2.2+0.8s cast, single-target, visual
    HydrobombAOE = 35490, // Helper->location, 3.0s cast, range 5 circle

    BlowingBubbles = 35464, // Boss->self, 2.2+0.8s cast, single-target, visual

    Hydroblast = 35491, // Boss->player, 5.0s cast, single-target, tankbuster
    HydroblastAOE = 35492, // Helper->player, no cast, range 5 circle

    Summon = 35470, // ZealBlindZozone->self, 3.0s cast, single-target
    WaterIII = 36116, // SummonedApa->self, 10.0s cast, single-target, proximity
    WaterIIIApply = 36125, // SummonedApa->player, no cast, single-target

    TidalWave = 36113, // Boss->self, 2.2+0.8s cast, single-target, visual
    TidalWaveAOE = 36114, // Helper->self, 6.0s cast, range 46 width 46 rect, knockback
    Hydrosurge = 35467, // Boss->self, 5.2+0.8s cast, single-target, visual
    HydrosurgeAOE = 35468, // Helper->self, 6.0s cast, single-target, starts persistent mid-line
    Wavefoam = 36115, // SummonedApa->self, 3.0s cast, single-target, spawn WavefoamBubble
    FlukeTyphoonAnila = 35471, // SummonedAnila->self, 8.0s cast, range 40 width 40 rect, knock bubbled players across

    Roar = 35474, // Boss->self, 5.0s cast, single-target, summon Zaratan
    BubbleNetSand = 35475, // Boss->self, 4.1+0.9s cast, single-target, visual (bubble one Zaratan)
    BubbleNetSandAOE = 35476, // Helper->self, 5.0s cast, range 65 circle, raidwide
    Updraft = 36111, // Boss->self, 6.5+0.5s cast, single-target, elevate bubbled Zaratan
    UpdraftAOE = 36112, // Helper->self, 7.0s cast, range 35 circle, visual
    HundredLashingsVisual = 35480, // AloaloZaratan->self, 14.4+1.1s cast, single-target, visual
    HundredLashings = 35479, // AloaloZaratan->self, 15.5s cast, range 60 180-degree cone (ground)
    HundredLashingsElevated = 35481, // Helper->self, 15.5s cast, single-target, elevated (misses)

    BubbleRiptide = 35458, // WavefoamBubble->player, no cast, single-target, pull into bubble
    BubbleFetters = 35459, // WavefoamBubble->player, no cast, single-target, bind in bubble
    UpdraftStone = 35477, // Boss->self, 6.0s cast, single-target, elevate bubble
    UpdraftStoneAOE = 35478, // Helper->self, 8.0s cast, range 35 circle, visual
    AerialShock = 35483, // AloaloOgrebon->self, 8.6+1.1s cast, single-target, visual
    AerialShockAOE = 35484, // Helper->self, 9.7s cast, range 35 circle

    Riptide = 35465, // AiryBubble->player, no cast, single-target
    Fetters = 35466, // AiryBubble->player, no cast, single-target
}

public enum SID : uint
{
    Bubble = 3745, // on bubbled spring crystals / Zaratan / Ogrebon (extra=0xC8)
    BubbleNet = 3744, // WavefoamBubble->player, extra=0x64
}

public enum TetherID : uint
{
    WaterIIIGood = 1, // Apa->player, stretched
    WaterIIIStretch = 57, // Apa->player, too close
}

sealed class Hydroblast(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Hydroblast);

sealed class WaterIII(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.WaterIII)
{
    private readonly List<Actor> _casters = [];
    private readonly List<(Actor Apa, Actor Target, bool Stretched)> _tethers = [];

    private const float MinStretch = 20;
    private static readonly AOEShapeCircle _tooClose = new(MinStretch);

    private readonly List<AOEInstance> _aoesBuffer = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        _aoesBuffer.Clear();
        foreach (var c in _casters)
            _aoesBuffer.Add(new(_tooClose, c.Position, default, Module.CastFinishAt(c.CastInfo)));
        return CollectionsMarshal.AsSpan(_aoesBuffer);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _casters.Add(caster);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _casters.RemoveAll(c => c == caster);
    }

    public override void OnTethered(Actor source, in ActorTetherInfo tether)
    {
        if (tether.ID is not ((uint)TetherID.WaterIIIGood or (uint)TetherID.WaterIIIStretch))
            return;
        if (source.OID != (uint)OID.SummonedApa || World.Actors.Find(tether.Target) is not { } target)
            return;

        _tethers.RemoveAll(t => t.Apa == source || t.Target == target);
        _tethers.Add((source, target, tether.ID == (uint)TetherID.WaterIIIGood));
    }

    public override void OnUntethered(Actor source, in ActorTetherInfo tether)
    {
        if (source.OID == (uint)OID.SummonedApa)
            _tethers.RemoveAll(t => t.Apa == source);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        foreach (var t in _tethers)
        {
            if (!(t.Target == actor))
                continue;
            if (!t.Stretched)
                hints.Add("Stretch tether!");
            else
                hints.Add("Tether is stretched!", false);
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        base.AddAIHints(slot, actor, assignment, hints);
        foreach (var t in _tethers)
        {
            if (!(t.Target == actor && !t.Stretched))
                continue;
            var activation = t.Apa.CastInfo != null ? Module.CastFinishAt(t.Apa.CastInfo) : default;
            hints.AddForbiddenZone(_tooClose.Distance(t.Apa.Position, default), activation);
        }
    }

    public override void DrawArenaForeground(int pcSlot, Actor pc)
    {
        foreach (var t in _tethers)
        {
            var color = t.Stretched ? Colors.Safe : Colors.Danger;
            Arena.AddLine(t.Apa.Position, t.Target.Position, color);
        }
    }
}

sealed class TidalWave(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.TidalWaveAOE, 27, ignoreImmunes: true, shape: new AOEShapeRect(46, 23), kind: Kind.DirForward)
{
    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        foreach (var c in Casters)
        {
            if (IsImmune(slot, c.Activation))
                continue;
            // stay near wave origin
            var origin = c.Origin;
            var activation = c.Activation;
            hints.AddForbiddenZone(new SDInvertedCircle(origin, 8), activation);
            hints.GoalZones.Add(AIHints.GoalProximity(origin, 4, 60));
        }
    }
}

sealed class Hydrosurge(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance? _aoe;
    private static readonly AOEShapeRect _shape = new(20, 5, 20);

    public bool Active => _aoe != null;

    private readonly List<AOEInstance> _aoesBuffer = [];
    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        _aoesBuffer.Clear();
        if (_aoe is { } aoe)
            _aoesBuffer.Add(aoe);
        return CollectionsMarshal.AsSpan(_aoesBuffer);
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.HydrosurgeAOE)
            _aoe = new(_shape, caster.Position, spell.Rotation, World.CurrentTime);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.FlukeTyphoonAnila)
            _aoe = null;
    }
}

sealed class FlukeTyphoonAnila(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.FlukeTyphoonAnila, 20, ignoreImmunes: true, shape: new AOEShapeRect(40, 20), kind: Kind.DirForward);

