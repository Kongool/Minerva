// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Foray.Hydatos.Ovni;

public enum OID : uint
{
    Boss = 0x2685, // R16.0
    UnfoundOgre = 0x26BF, // R1.3
}

public enum AID : uint
{
    AutoAttack1 = 14781, // Boss->player, no cast, single-target
    AutoAttack2 = 15228, // UnfoundOgre->player, no cast, single-target

    RockHard = 14786, // Boss->location, 3.0s cast, range 8 circle
    TorrentialTorment = 14785, // Boss->self, 5.0s cast, range 40+R 45-degree cone
    Fluorescence = 14789, // Boss->self, 3.0s cast, single-target
    PullOfTheVoid = 14782, // Boss->self, 3.0s cast, range 30 circle
    Megastorm = 14784, // Boss->self, 5.0s cast, range ?-40 donut
    IonShower = 14787, // Boss->player, 5.0s cast, single-target
    IonStorm = 14788, // Boss->players, no cast, range 20 circle
    VitriolicBarrage = 14790, // Boss->self, 4.0s cast, range 25+R circle
    ConcussiveOscillation = 14783, // Boss->self, 5.0s cast, range 24 circle
    StraightPunch = 15430, // UnfoundOgre->player, no cast, single-target
    PlainPound = 15431 // UnfoundOgre->self, 3.0s cast, range 6 circle
}

public enum IconID : uint
{
    IonShower = 111 // player->self
}

public enum SID : uint
{
    DamageUp = 1766 // Boss->Boss, extra=0x1
}

class PullOfTheVoid(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.PullOfTheVoid, 30f, shape: new AOEShapeCircle(30f), kind: Kind.TowardsOrigin, minDistanceBetweenHitboxes: true);
class Megastorm(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Megastorm, new AOEShapeDonut(5f, 40f));
class ConcussiveOscillation(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ConcussiveOscillation, 24f);
class VitriolicBarrage(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.VitriolicBarrage);
class RockHard(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RockHard, 8);
class TorrentialTorment(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.TorrentialTorment, new AOEShapeCone(56f, 22.5f.Degrees()));
class Fluorescence(ModuleBase module) : Components.Dispel(module, (uint)SID.DamageUp);
class IonShower(ModuleBase module) : Components.GenericStackSpread(module, raidwideOnResolve: false)
{
    private int _numCasts;

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.IonShower && World.Actors.Find(targetID) is { } target)
        {
            _numCasts = 0;
            Spreads.Add(new(target, 20f, World.FutureTime(5d)));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.IonStorm && ++_numCasts >= 3)
            Spreads.Clear();
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var found = false;
        var count = Spreads.Count;
        var spreads = CollectionsMarshal.AsSpan(Spreads);
        for (var i = 0; i < count; ++i)
        {
            if (spreads[i].Target == actor)
            {
                found = true;
                break;
            }
        }
        if (found) // just gtfo from boss as far as possible
        {
            var pos = Module.PrimaryActor.Position;
            hints.GoalZones.Add(p => (p - pos).LengthSq() > 1600f ? 100f : default);
        }
        else
        {
            base.AddAIHints(slot, actor, assignment, hints);
        }
    }
}

class PlainPound(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.PlainPound, 6);

class OvniStates : StateMachineBuilder
{
    public OvniStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<PullOfTheVoid>()
            .ActivateOnEnter<Megastorm>()
            .ActivateOnEnter<ConcussiveOscillation>()
            .ActivateOnEnter<VitriolicBarrage>()
            .ActivateOnEnter<RockHard>()
            .ActivateOnEnter<TorrentialTorment>()
            .ActivateOnEnter<IonShower>()
            .ActivateOnEnter<Fluorescence>()
            .ActivateOnEnter<PlainPound>();
    }
}

[ModuleInfo(Group = ModuleGroup.EurekaNM, CFCID = 639u, NameID = 1424u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "xan, Malediktus (ported from BMR)")]
public class Ovni(WorldState ws, Actor primary) : SimpleBossModule(ws, primary)
{
    protected override void DrawEnemies(int pcSlot, Actor pc)
    {
        Arena.Actor(PrimaryActor);
        Arena.Actors(Enemies((uint)OID.UnfoundOgre));
    }

    protected override void CalculateModuleAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = hints.PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var e = hints.PotentialTargets[i];
            e.Priority = e.Actor.OID switch
            {
                (uint)OID.Boss => 1,
                _ when e.Actor.InCombat => 0,
                _ => AIHints.Enemy.PriorityUndesirable
            };
        }
    }
}
