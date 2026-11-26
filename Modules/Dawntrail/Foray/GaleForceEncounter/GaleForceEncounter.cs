// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.FATE.GaleForceEncounter;

public enum OID : uint
{
    Stormcaller = 0x4BEC,
    Helper = 0x233C,
    StormcallerHelper = 0x4BED, // R1.000, x0 (spawn during fight)
    BitingWind = 0x4C25, // R1.000, x0 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 50854, // Stormcaller->player, no cast, single-target
    Teleport = 45587, // Stormcaller->location, no cast, single-target
    Stormcall = 47580, // Stormcaller->self, 3.0s cast, single-target
    FreefallTeleport = 47598, // Stormcaller->location, 4.0s cast, single-target
    Freefall = 47584, // Stormcaller->location, no cast, range 12 circle
    BitingScratch = 47588, // Stormcaller->self, 5.0s cast, range 40 90.000-degree cone
    Windage = 47583, // 4C25->self, 2.0s cast, range 7 circle

    FocusedTremor1 = 47587, // 4BED->location, 6.0s cast, range 10 circle
    FocusedTremor2 = 47586, // 4BED->location, 8.0s cast, range 10-20 donut
    FocusedTremor3 = 47585, // 4BED->location, 10.0s cast, range 20-30 donut

    FocusedTremor4 = 47594, // 4BED->location, 9.0s cast, range 10 circle
    FocusedTremor5 = 47593, // 4BED->location, 11.0s cast, range 10-20 donut
    FocusedTremor6 = 47592, // 4BED->location, 13.0s cast, range 20-30 donut

    FocusedTremor7 = 47597, // 4BED->location, 11.5s cast, range 10 circle
    FocusedTremor8 = 47596, // 4BED->location, 13.5s cast, range 10-20 donut
    FocusedTremor9 = 47595, // 4BED->location, 15.5s cast, range 20-30 donut
}

sealed class Windage(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Windage, new AOEShapeCircle(7.0f));
sealed class BitingScratch(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.BitingScratch, new AOEShapeCone(40.0f, 45.0f.Degrees()));

sealed class FocusedTremor(ModuleBase module) : Components.ConcentricAOEs(module, _shapes)
{
    private static readonly AOEShape[] _shapes = [new AOEShapeCircle(10f), new AOEShapeDonut(10f, 20f), new AOEShapeDonut(20f, 30f)];

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.FocusedTremor1 or (uint)AID.FocusedTremor4 or (uint)AID.FocusedTremor7)
        {
            AddSequence(spell.LocXZ, Module.CastFinishAt(spell));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (Sequences.Count != 0)
        {
            var order = spell.Action.ID switch
            {
                (uint)AID.FocusedTremor1 or (uint)AID.FocusedTremor4 or (uint)AID.FocusedTremor7 => 0,
                (uint)AID.FocusedTremor2 or (uint)AID.FocusedTremor5 or (uint)AID.FocusedTremor8 => 1,
                (uint)AID.FocusedTremor3 or (uint)AID.FocusedTremor6 or (uint)AID.FocusedTremor9 => 2,
                _ => -1
            };

            AdvanceSequence(order, spell.LocXZ, World.FutureTime(2d));
        }
    }

    // A toon that rides into the FATE while the rings are being cast never sees the casts start: the game sends a cast
    // once, when it begins, so for a caster that loads in afterwards there is no cast bar, no telegraph, and nothing
    // above to start or advance a sequence. The casts are 6 to 15.5 seconds long and the rings land in order two
    // seconds apart, so the first ring seen to land tells where the rest will. Stormcaller, 2026-11-19 21:05: Saar
    // arrived 5s into a set, walked in under a 20-30y ring and a 10-20y ring nothing had drawn, and died at 8.8s;
    // 12 of the 23 cast-bar hits in 31 recordings were rings like these, with four deaths.
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        var order = spell.Action.ID switch
        {
            (uint)AID.FocusedTremor1 or (uint)AID.FocusedTremor4 or (uint)AID.FocusedTremor7 => 0,
            (uint)AID.FocusedTremor2 or (uint)AID.FocusedTremor5 or (uint)AID.FocusedTremor8 => 1,
            (uint)AID.FocusedTremor3 or (uint)AID.FocusedTremor6 or (uint)AID.FocusedTremor9 => 2,
            _ => -1
        };
        if (order < 0)
        {
            return;
        }
        var origin = spell.TargetXZ;
        var next = World.FutureTime(2d);
        // a sequence waiting on this very ring: its cast's end was not seen (a cast seen to end has moved it on already)
        if (AdvanceSequence(order, origin, next))
        {
            return;
        }
        foreach (var known in Sequences)
        {
            if (known.Origin.AlmostEqual(origin, 1f))
            {
                return;
            }
        }
        if (order < _shapes.Length - 1)
        {
            AddSequence(origin, next);
            Sequences[^1].Done = order + 1;
        }
    }
}

// Freefall has no cast of its own. The Stormcaller's teleport cast (4s) is followed by three landings 2.85s apart, each a
// 12y circle, and each on the centre of one of the three Focused Tremor sets cast with it: 2.26s after the 6s ring's
// cast starts, 5.11s after the 9s ring's, 7.93s after the 11.5s ring's (31 landings in 13 pulls, within 0.16s). Neither
// this module nor BossmodReborn's drew it, and it hit 16 players in 50 landings.
sealed class Freefall(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle circle = new(12f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        var landsAfter = spell.Action.ID switch
        {
            (uint)AID.FocusedTremor1 => 2.26f,
            (uint)AID.FocusedTremor4 => 5.11f,
            (uint)AID.FocusedTremor7 => 7.93f,
            _ => 0f
        };
        // a cast met partway through (the module starting late) may already be past its landing
        var wait = landsAfter - spell.ElapsedTime;
        if (wait > 0f)
        {
            _aoes.Add(new(circle, spell.LocXZ, default, World.FutureTime(wait)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Freefall)
        {
            var landed = spell.TargetXZ;
            _aoes.RemoveAll(aoe => aoe.Origin.AlmostEqual(landed, 2f));
        }
    }

    public override void Update()
    {
        // a landing that never came (the boss died mid-cast): gone a second and a half after its time
        if (_aoes.Count != 0)
        {
            var stale = World.CurrentTime.AddSeconds(-1.5d);
            _aoes.RemoveAll(aoe => aoe.Activation < stale);
        }
    }
}

[SkipLocalsInit]
sealed class GaleForceEncounterStates : StateMachineBuilder
{
    public GaleForceEncounterStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Windage>()
            .ActivateOnEnter<BitingScratch>()
            .ActivateOnEnter<FocusedTremor>()
            .ActivateOnEnter<Freefall>();
    }
}

[ModuleInfo(Group = ModuleGroup.ForayFATE, GroupID = 1093u, CFCID = 1093u, NameID = 2082u, PrimaryActorOID = (uint)OID.Stormcaller, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
// BMR derives this from OpenWorldFate, which follows the boss and gates activation on the player being
// within 30y. Minerva takes a fixed centre, so this is the centre of the cast locations across a real pull
// (Stormcaller, recording 2026-08-24: 129 ground casts spanning 44x44y) with BMR's own 30y FATE radius.
public sealed class GaleForceEncounter(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-857.7f, 478.9f), new ArenaBoundsCircle(30f));
