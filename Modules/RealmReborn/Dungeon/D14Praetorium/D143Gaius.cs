// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D14Praetorium.D143Gaius;

public enum OID : uint
{
    Boss = 0x3875, // x1

    PhantomGaiusSide = 0x3876, // R1.65, untargetable
    PhantomGaiusAdd = 0x3877, // R1.65, adds that become targetable on low hp
    TerminusEst = 0x3878, // R=1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target

    Phantasmata = 28484, // Boss->self, 3.0s cast, single-target, visual (shows 'side' untargetable adds)
    PhantasmataShow = 28485, // PhantomGaiusSide/PhantomGaiusAdd->location, no cast, single-target (become visible)
    TerminusEstSummon = 28486, // Boss/PhantomGaiusSide->self, no cast, single-target, visual (show 'X' and spawn TerminusEst)
    TerminusEstTriple = 28487, // TerminusEst->self, 3.0s cast, range 40 width 4 rect, always three neighbouring casters
    TerminusEstQuintuple = 28488, // TerminusEst->self, 3.0s cast, range 40 width 4 rect, always five casters with gaps between
    TerminusEstVisual = 29779, // Helper->self, 6.0s cast, range 40 width 12 rect, no effect?..

    HandOfTheEmpire = 28491, // Boss/PhantomGaiusAdd->self, 3.0s cast, single-target, visual
    HandOfTheEmpireAOE = 28492, // Helper->player, 5.0s cast, range 5 circle spread
    FestinaLente = 28493, // Boss->player, 5.0s cast, range 6 circle stack
    Innocence = 28494, // Boss->player, 5.0s cast, single-target tankbuster
    HorridaBella = 28495, // Boss->self, 5.0s cast, raidwide
    Teleport = 28496, // Boss->location, no cast, single-target

    Ductus = 29051, // Boss/PhantomGaiusAdd->self, 3.0s cast, single-target, visual
    DuctusAOE = 29052, // Helper->location, 5.0s cast, range 8 circle aoe (pseudo exaflare)

    AddPhaseStart = 28497, // Boss->self, no cast, single-target, visual (enemy 'lb' gauge starts filling over 90 secs)
    Heirsbane = 28498, // PhantomGaiusAdd->player, 5.0s cast, single-target damage
    VeniVidiVici = 28499, // Boss->self, no cast, raidwide on last add death
    VeniVidiViciEnrage = 28500 // Boss->self, no cast, enrage (if adds aren't killed in 90s)
}

class TerminusEst(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShapeRect rect = new(40f, 2f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.TerminusEst)
            _aoes.Add(new(rect, actor.Position.Quantized(), actor.Rotation, World.FutureTime(6d)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.TerminusEstTriple or (uint)AID.TerminusEstQuintuple)
            _aoes.Clear();
    }
}

class HandOfTheEmpire(ModuleBase module) : Components.SpreadFromCastTargets(module, (uint)AID.HandOfTheEmpireAOE, 5f);
class FestinaLente(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.FestinaLente, 6f, 4, 4);
class Innocence(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Innocence);
class HorridaBella(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.HorridaBella);
class Ductus(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.DuctusAOE, 8f);

class AddEnrage(ModuleBase module) : ModuleComponent(module)
{
    private DateTime _enrage;

    public override void AddGlobalHints(GlobalHints hints)
    {
        if (_enrage != default)
            hints.Add($"Enrage in {(_enrage - World.CurrentTime).TotalSeconds:f1}s");
    }

    public override void Update()
    {
        var primary = Module.PrimaryActor.IsTargetable;
        var enrage = _enrage == default;
        if (enrage && !primary)
            _enrage = World.FutureTime(92.4d);
        else if (!enrage && primary)
            _enrage = default;
    }
}

class Heirsbane(ModuleBase module) : Components.SingleTargetCast(module, (uint)AID.Innocence, "");

class D143GaiusStates : StateMachineBuilder
{
    public D143GaiusStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<TerminusEst>()
            .ActivateOnEnter<HandOfTheEmpire>()
            .ActivateOnEnter<FestinaLente>()
            .ActivateOnEnter<Innocence>()
            .ActivateOnEnter<HorridaBella>()
            .ActivateOnEnter<Ductus>()
            .ActivateOnEnter<AddEnrage>()
            .ActivateOnEnter<Heirsbane>();
    }
}

[ModuleInfo(CFCID = 16u, NameID = 2136u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Malediktus (ported from BMR)")]
public class D143Gaius(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-562f, 220f), new ArenaBoundsRect(14.5f, 19.5f))
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.PhantomGaiusAdd));
    }
}
