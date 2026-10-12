// Written from two recordings of one pull (Korha and Saar, 2026-10-11, a wipe); BossmodReborn has no module for this
// dungeon. Every shape here is measured from who was hit and who was not, not read off a datasheet.
//
// Not handled:
//  - The doors. Ruinous Omen has to be answered by opening a matching pair of the eight Diabolical Gates on the wall,
//    and that pull opened none, so nothing is known about which doors match or what an opened one shows. The cast is
//    announced and the doors are left to the humans.
//  - Camisado (the tank's hit) and Noctoshield: nothing to stand out of.
//  - Where on the wall an orb is best left. One beside a door will be in the way of whoever has to open it.
using System;
using System.Collections.Generic;
using Minerva;

namespace Minerva.RealmReborn.Dungeon.D20LostCityOfAmdapor.D203Diabolos;

public enum OID : uint
{
    Diabolos = 0xA7C, // R1.5
    GravityOrb = 0xA8B, // R1.5, left where Graviball lands, for the rest of the fight
    DiabolicGate = 0xB84 // two of them at (0, -22.1)
}

public enum AID : uint
{
    AutoAttack = 1882, // Diabolos->tank, no cast, a 90-degree cone in front: it hit at 0-41 degrees off and 8.8y, missed at 56
    Camisado = 1883, // Diabolos->tank, no cast, single-target
    Nightmare = 1884, // Diabolos->self, 3.7s cast, sleeps everyone whichever way they face
    Graviball = 1890, // Diabolos->player, 1.7s cast, single-target; a Gravity Orb appears where the player stands at the end
    CriticalGravity = 1891, // Gravity Orb->self, no cast: hit at up to 6.0y, missed at 8.7y
    Noctoshield = 1892, // Diabolos->self
    RuinousOmen = 1893, // Diabolos->self, 14.7s cast, raidwide; the doors answer it
    UltimateTerror = 1894 // Diabolos->self, 3.7s cast, donut: clear at 7.1-8.2y, hit at 10.4y and at 21.0y
}

sealed class AutoAttack(ModuleBase module) : Components.Cleave(module, (uint)AID.AutoAttack, new AOEShapeCone(10.5f, 45f.Degrees()));
sealed class UltimateTerror(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.UltimateTerror, new AOEShapeDonut(8f, 30f));
sealed class Nightmare(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Nightmare, "Sleep on everyone");
sealed class RuinousOmen(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.RuinousOmen, "Open a matching pair of doors");

/// <summary>
/// The orb is left where the marked player stands when the cast ends, and it never goes away. Both orbs of the
/// recorded pull were dropped at the boss's feet, which is the one place it must not be: Ultimate Terror's only clear
/// ground is the eight yalms around the boss, and an orb there pulses over all of it. So the marked player carries it
/// out, far enough that the orb's circle and that hole do not touch.
/// <para>1.7s is about ten yalms of running, so someone marked in melee range leaves it short of that. Ten yalms out
/// still frees most of the hole, and the zone is real danger rather than an assigned spot so that the run starts at
/// once and costs a cast: an orb in the middle is paid for all fight.</para>
/// </summary>
sealed class Graviball(ModuleBase module) : ModuleComponent(module)
{
    /// <summary>Ultimate Terror's hole plus the orb's own reach.</summary>
    private const float ClearOfTheHole = 16f;

    private Actor? target;
    private DateTime lands;

    public override void OnCastStarted(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == (uint)AID.Graviball)
        {
            target = World.Actors.Find(cast.TargetID);
            lands = Module.CastFinishAt(cast);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo cast)
    {
        if (cast.Action.ID == (uint)AID.Graviball)
        {
            target = null;
        }
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (actor == target)
        {
            hints.AddForbiddenZone(new SDCircle(Module.PrimaryActor.Position, ClearOfTheHole), lands);
        }
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        if (actor == target)
        {
            hints.Add("Carry the orb to the wall!");
        }
    }
}

/// <summary>
/// Each orb pulses on its own clock with no cast bar: 5.0s and 4.9s after appearing, then every 15.0-15.3s. Both
/// numbers are taken a little short, and a pulse that is late stays due until it is seen.
/// </summary>
sealed class CriticalGravity(ModuleBase module) : Components.GenericAOEs(module)
{
    private const double FirstPulse = 4.8d;
    private const double BetweenPulses = 15d;

    private static readonly AOEShapeCircle circle = new(8f);
    // an orb that was already there when the module started is not in here, and counts as due now
    private readonly Dictionary<ulong, DateTime> nextPulse = [];
    private AOEInstance[] _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoes;

    public override void Update()
    {
        var orbs = Module.Enemies((uint)OID.GravityOrb);
        var count = 0;
        for (var i = 0; i < orbs.Count; ++i)
        {
            if (!orbs[i].IsDeadOrDestroyed)
            {
                ++count;
            }
        }
        if (_aoes.Length != count)
        {
            _aoes = new AOEInstance[count];
        }
        var index = 0;
        for (var i = 0; i < orbs.Count; ++i)
        {
            var orb = orbs[i];
            if (!orb.IsDeadOrDestroyed)
            {
                _aoes[index++] = new(circle, orb.Position, default, nextPulse.GetValueOrDefault(orb.InstanceID));
            }
        }
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.GravityOrb)
        {
            nextPulse[actor.InstanceID] = World.FutureTime(FirstPulse);
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        nextPulse.Remove(actor.InstanceID);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.CriticalGravity)
        {
            nextPulse[caster.InstanceID] = World.FutureTime(BetweenPulses);
        }
    }
}

sealed class D203DiabolosStates : StateMachineBuilder
{
    public D203DiabolosStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AutoAttack>()
            .ActivateOnEnter<UltimateTerror>()
            .ActivateOnEnter<Nightmare>()
            .ActivateOnEnter<RuinousOmen>()
            .ActivateOnEnter<Graviball>()
            .ActivateOnEnter<CriticalGravity>();
    }
}

[ModuleInfo(Group = ModuleGroup.CFC, GroupID = 22u, CFCID = 22u, NameID = 2564u, PrimaryActorOID = (uint)OID.Diabolos, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "Minerva (from recordings)")]
// the eight doors stand on a ring 22.1y from the middle of the room, which is the wall
public sealed class D203Diabolos(WorldState ws, Actor primary) : ModuleBase(ws, primary, new WPos(0f, 0f), new ArenaBoundsCircle(22f));
