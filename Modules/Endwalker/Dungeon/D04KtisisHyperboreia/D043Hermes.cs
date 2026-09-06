// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D04KtisisHyperboreia.D043Hermes;

public enum OID : uint
{
    Boss = 0x348A, // R4.2
    Meteor = 0x348C, // R2.4
    Karukeion = 0x348B, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 872, // Boss->player, no cast, single-target

    CosmicKiss = 25891, // Meteor->self, 5.0s cast, range 40 circle
    Double = 25892, // Boss->self, 3.0s cast, single-target

    Hermetica1 = 25888, // Boss->self, 3.0s cast, single-target
    Hermetica2 = 25893, // Boss->self, 6.0s cast, single-target
    Hermetica3 = 25895, // Boss->self, 12.0s cast, single-target

    Meteor = 25890, // Boss->self, 3.0s cast, single-target
    Quadruple = 25894, // Boss->self, 3.0s cast, single-target
    Trismegistos = 25886, // Boss->self, 5.0s cast, range 40 circle, raidwide

    TrueAeroVisual = 25899, // Boss->self, 5.0s cast, single-target
    TrueAeroTarget = 25887, // Helper->player, no cast, single-target
    TrueAeroFirst = 25900, // Helper->player, no cast, range 40 width 6 rect
    TrueAeroRepeat = 25901, // Helper->self, 2.5s cast, range 40 width 6 rect

    TrueAeroII1 = 25896, // Boss->self, 5.0s cast, single-target
    TrueAeroII2 = 25897, // Helper->player, 5.0s cast, range 6 circle
    TrueAeroII3 = 25898, // Helper->location, 3.5s cast, range 6 circle

    TrueAeroIV1 = 25889, // Karukeion->self, 4.0s cast, range 50 width 10 rect
    TrueAeroIVLOS = 27836, // Karukeion->self, 4.0s cast, range 50 width 10 rect
    TrueAeroIV3 = 27837, // Karukeion->self, 10.0s cast, range 50 width 10 rect

    TrueBravery = 25907, // Boss->self, 5.0s cast, single-target

    TrueTornado1 = 25902, // Boss->self, 5.0s cast, single-target
    TrueTornado2 = 25903, // Boss->self, no cast, single-target
    TrueTornado3 = 25904, // Boss->self, no cast, single-target
    TrueTornado4 = 25905, // Helper->player, no cast, range 4 circle
    TrueTornadoAOE = 25906 // Helper->location, 2.5s cast, range 4 circle
}

public enum IconID : uint
{
    Tankbuster = 218 // player
}

sealed class TrismegistosArenaChange(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeDonut donut = new(20f, 22f);
    private AOEInstance[] _aoe = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Trismegistos && Bounds.Radius > 20f)
        {
            _aoe = [new(donut, Center, default, Module.CastFinishAt(spell, 0.5d))];
        }
    }

    public override void OnMapEffect(byte index, uint state)
    {
        if (index == 0x08 && state == 0x00020001u)
        {
            Bounds = D043Hermes.DefaultBounds;
            _aoe = [];
        }
    }
}

sealed class TrueBraveryInterruptHint(ModuleBase module) : Components.CastInterruptHint(module, (uint)AID.TrueBravery);
sealed class Trismegistos(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Trismegistos);

sealed class TrueTornadoTankbuster(ModuleBase module) : Components.BaitAwayIcon(module, 4f, (uint)IconID.Tankbuster, (uint)AID.TrueTornado4, 5.1d, tankbuster: true);

sealed class TrueTornadoAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TrueTornadoAOE, 4f);

sealed class TrueAeroFirst(ModuleBase module) : Components.GenericBaitAway(module)
{
    private static readonly AOEShapeRect rect = new(40f, 3f);
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.TrueAeroTarget)
            CurrentBaits.Add(new(Module.PrimaryActor, World.Actors.Find(spell.MainTargetID)!, rect, World.FutureTime(5.7d)));
        else if (spell.Action.ID == (uint)AID.TrueAeroFirst)
            CurrentBaits.Clear();
    }
}

sealed class TrueAeroRepeat(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TrueAeroRepeat, new AOEShapeRect(40f, 3f));

sealed class TrueAeroII2(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.TrueAeroII2, 6f);
sealed class TrueAeroII3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TrueAeroII3, 6f);

sealed class TrueAeroIV1(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TrueAeroIV1, new AOEShapeRect(50f, 5f));
sealed class TrueAeroIV3(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TrueAeroIV3, new AOEShapeRect(50f, 5f), 4);

sealed class CosmicKiss(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.CosmicKiss, 10f);

sealed class TrueAeroIVLOS(ModuleBase module) : Components.CastLineOfSightAOE(module, (uint)AID.TrueAeroIVLOS, 50f, false, true)
{
    private readonly CosmicKiss _aoe = module.FindComponent<CosmicKiss>()!;

    public override ReadOnlySpan<Actor> BlockerActors()
    {
        var meteors = Module.Enemies((uint)OID.Meteor);
        var count = meteors.Count;
        if (count != 0)
        {
            for (var i = 0; i < count; ++i)
            {
                var m = meteors[i];
                if (m.ModelState.AnimState2 != 1)
                {
                    return new Actor[1] { m };
                }
            }
            return [];
        }
        return [];
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var meteors = Module.Enemies((uint)OID.Meteor);
        var count = meteors.Count;
        if (count == 0)
            return;
        base.AddAIHints(slot, actor, assignment, hints);

        Actor? meteor = null;
        var countBroken = 0;
        for (var i = 0; i < count; ++i)
        {
            var m = meteors[i];
            if (m.ModelState.AnimState2 != 1)
                meteor = m;
            else
                ++countBroken;
        }

        if (countBroken == 3 && meteor is Actor met && _aoe.Casters.Count == 0) // force AI to move closer to the meteor as soon as they become visible
        {
            hints.GoalZones.Add(AIHints.GoalSingleTarget(met, 5f, 5f));
        }
    }
}

sealed class D043HermesStates : StateMachineBuilder
{
    public D043HermesStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TrismegistosArenaChange>()
            .ActivateOnEnter<TrueBraveryInterruptHint>()
            .ActivateOnEnter<CosmicKiss>()
            .ActivateOnEnter<TrueAeroFirst>()
            .ActivateOnEnter<TrueAeroRepeat>()
            .ActivateOnEnter<TrueAeroII2>()
            .ActivateOnEnter<TrueAeroII3>()
            .ActivateOnEnter<TrueAeroIV1>()
            .ActivateOnEnter<TrueAeroIVLOS>()
            .ActivateOnEnter<TrueAeroIV3>()
            .ActivateOnEnter<TrueTornadoTankbuster>()
            .ActivateOnEnter<TrueTornadoAOE>()
            .ActivateOnEnter<Trismegistos>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 787u, CFCID = 787u, NameID = 10363u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public sealed class D043Hermes(WorldState ws, Actor primary) : ModuleBase(ws, primary, ArenaCenter, StartingBounds)
{
    private static readonly WPos ArenaCenter = new(default, -50f);
    public static readonly ArenaBoundsCustom StartingBounds = new([new Polygon(ArenaCenter, 21.5f, 64)]);
    public static readonly ArenaBoundsCustom DefaultBounds = new([new Polygon(ArenaCenter, 20f, 64)]);
}
