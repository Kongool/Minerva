// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D13LunarSubterrane.D133Durante;

public enum OID : uint
{
    Boss = 0x4042, // R=6.0
    AethericCharge = 0x4043, // R=1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    Teleport = 34991, // Boss->location, no cast, single-target

    ArcaneEdge = 35010, // Boss->player, 5.0s cast, single-target
    OldMagic = 35011, // Boss->self, 5.0s cast, range 60 circle

    DuplicitousBatteryTelegraph = 36058, // Helper->location, 3.5s cast, range 5 circle
    DuplicitousBatteryVisual = 36057, // Boss->self, 6.0s cast, single-target
    DuplicitousBattery = 34994, // Helper->location, no cast, range 5 circle

    ForsakenFount = 35003, // Boss->self, 3.0s cast, single-target
    Explosion1 = 35006, // 4043->self, 5.0s cast, range 11 circle
    Explosion2 = 35005, // Helper->self, 12.0s cast, range 9 circle
    FallenGrace = 35882, // Helper->player, 5.0s cast, range 6 circle
    Contrapasso = 35905, // Boss->self, 3.0s cast, range 60 circle
    Splinter = 35004, // AethericCharge->self, no cast, single-target
    HardSlash = 35009, // Boss->self, 5.0s cast, range 50 90-degree cone

    AntipodalAssaultMarker = 14588, // Helper->player, no cast, single-target
    AntipodalAssaultVisual = 35007, // Boss->self, 5.0s cast, single-target, line stack
    AntipodalAssault = 35008, // Boss->location, no cast, width 8 rect charge

    TwilightPhaseVisual1 = 36055, // Boss->self, 6.0s cast, single-target
    TwilightPhaseVisual2 = 34997, // Boss->self, no cast, single-target
    TwilightPhaseVisual3 = 34998, // Boss->self, no cast, single-target
    TwilightPhase = 36056, // Helper->self, 7.3s cast, range 60 width 20 rect

    DarkImpactVisual = 35001, // Boss->location, 7.0s cast, single-target
    DarkImpact = 35002, // Helper->self, 8.0s cast, range 25 circle

    DeathsJourneyVisual = 35872, // Helper->self, 6.5s cast, range 30 30-degree cone
    DeathsJourneyCircle = 34995, // Boss->self, 6.0s cast, range 8 circle
    DeathsJourneyCone = 34996 // Helper->self, 6.5s cast, range 30 30-degree cone
}

sealed class ArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(20f, 23f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.OldMagic && Bounds.Radius > 20f)
        {
            _aoe = [new(donut, Center, default, Module.CastFinishAt(spell))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x0A && state == 0x00020001u)
        {
            Bounds = D133Durante.DefaultBounds;
            _aoe = [];
        }
    }
}

sealed class OldMagicArcaneEdgeContrapasso(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.OldMagic, (uint)AID.ArcaneEdge, (uint)AID.Contrapasso]);

sealed class DuplicitousBattery(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeCircle circle = new(5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0)
            return [];
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var max = count > 16 ? 16 : count;
        var color = Colors.Danger;
        for (var i = 0; i < max; ++i)
        {
            ref var aoe = ref aoes[i];
            if (count > 16 && i < 16)
                aoe.Color = color;
            aoe.Risky = true;
        }
        return aoes;
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DuplicitousBatteryTelegraph)
            _aoes.Add(new(circle, spell.LocXZ, default, World.FutureTime(6.5d), risky: false));

    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.DuplicitousBattery)
            _aoes.RemoveAt(0);
    }
}

sealed class Explosion1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion1, 11f);
sealed class Explosion2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Explosion2, 9f);
sealed class FallenGrace(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.FallenGrace, 6f);
sealed class AntipodalAssault(ModuleBase module) : Components.LineStack(module, aidMarker: (uint)AID.AntipodalAssaultMarker, (uint)AID.AntipodalAssault, 5.4d, markerIsFinalTarget: false);
sealed class HardSlash(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.HardSlash, new AOEShapeCone(50f, 45f.Degrees()));
sealed class TwilightPhase(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TwilightPhase, new AOEShapeRect(60f, 10f));
sealed class DarkImpact(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DarkImpact, 25f);
sealed class DeathsJourneyCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DeathsJourneyCircle, 8f);
sealed class DeathsJourneyCone(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DeathsJourneyCone, new AOEShapeCone(30f, 15f.Degrees()));

sealed class D133DuranteStates : StateMachineBuilder
{
    public D133DuranteStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<ArenaChange>()
            .ActivateOnEnter<OldMagicArcaneEdgeContrapasso>()
            .ActivateOnEnter<DuplicitousBattery>()
            .ActivateOnEnter<Explosion1>()
            .ActivateOnEnter<Explosion2>()
            .ActivateOnEnter<FallenGrace>()
            .ActivateOnEnter<AntipodalAssault>()
            .ActivateOnEnter<HardSlash>()
            .ActivateOnEnter<TwilightPhase>()
            .ActivateOnEnter<DarkImpact>()
            .ActivateOnEnter<DeathsJourneyCircle>()
            .ActivateOnEnter<DeathsJourneyCone>();
    }
}

[ModuleInfo(CFCID = 823u, NameID = 12584u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
sealed class D133Durante(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(default, -422f), StartingBounds)
{
    public static readonly ArenaBounds StartingBounds = new ArenaBoundsCircle(22.5f);
    public static readonly ArenaBounds DefaultBounds = new ArenaBoundsCircle(20f);
}
