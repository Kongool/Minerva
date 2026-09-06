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
}

[SkipLocalsInit]
sealed class GaleForceEncounterStates : StateMachineBuilder
{
    public GaleForceEncounterStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Windage>()
            .ActivateOnEnter<BitingScratch>()
            .ActivateOnEnter<FocusedTremor>();
    }
}

[ModuleInfo(CFCID = 1093u, NameID = 2082u, PrimaryActorOID = (uint)OID.Stormcaller, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Equilius (ported from BMR)")]
[SkipLocalsInit]
// BMR derives this from OpenWorldFate, which follows the boss and gates activation on the player being
// within 30y. Minerva takes a fixed centre, so this is the centre of the cast locations across a real pull
// (Stormcaller, recording 2026-08-24: 129 ground casts spanning 44x44y) with BMR's own 30y FATE radius.
public sealed class GaleForceEncounter(WorldState ws, Actor primary)
    : ModuleBase(ws, primary, new WPos(-857.7f, 478.9f), new ArenaBoundsCircle(30f));
