// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Dungeon.D05Aitiascope.D051Livia;

public enum OID : uint
{
    Boss = 0x3469, // R7.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 24771, // Boss->player, no cast, single-target

    AglaeaBite = 25673, // Boss->self/player, 5.0s cast, range 9 90-degree cone, tankbuster 

    AglaeaClimb1 = 25666, // Boss->self, 7.0s cast, single-target
    AglaeaClimb2 = 25667, // Boss->self, 7.0s cast, single-target
    AglaeaClimbAOE = 25668, // Helper->self, 7.0s cast, range 20 90-degree cone

    AglaeaShotVisual = 25669, // Boss->self, 3.0s cast, single-target
    AglaeaShot1 = 25670, // 346A->location, 3.0s cast, range 20 width 4 rect
    AglaeaShot2 = 25671, // 346A->location, 1.0s cast, range 40 width 4 rect

    Disparagement = 25674, // Boss->self, 5.0s cast, range 40 120-degree cone

    Frustration = 25672, // Boss->self, 5.0s cast, range 40 circle, raidwide

    IgnisAmoris = 25676, // Helper->location, 4.0s cast, range 6 circle
    IgnisOdi = 25677, // Helper->players, 5.0s cast, range 6 circle

    OdiEtAmo = 25675 // Boss->self, 3.0s cast, single-target
}

class AglaeaBite(ModuleBase module) : Components.BaitAwayCast(module, (uint)AID.AglaeaBite, new AOEShapeCone(9f, 60f.Degrees()), endsOnCastEvent: true, tankbuster: true);

class AglaeaShot(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private static readonly AOEShapeRect rect = new(20f, 3f);
    private readonly List<Actor> casters = [];
    private DateTime activation;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_aoes.Count != 0)
            return CollectionsMarshal.AsSpan(_aoes);
        if ((activation - World.CurrentTime).TotalSeconds < 5d)
        {
            var count = casters.Count;
            var aoes = new AOEInstance[count];
            for (var i = 0; i < count; ++i)
            {
                var c = casters[i];
                aoes[i] = new(rect, c.Position, c.Rotation, activation);
            }
            return aoes;
        }
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.AglaeaShot1 or (uint)AID.AglaeaShot2)
        {
            _aoes.Add(new(rect, spell.LocXZ, spell.Rotation, Module.CastFinishAt(spell)));
            casters.Clear();
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0)
        {
            if (spell.Action.ID == (uint)AID.AglaeaShot1)
            {
                activation = World.FutureTime(10d);
                _aoes.RemoveAt(0);
                casters.Add(caster);
            }
            else if (spell.Action.ID == (uint)AID.AglaeaShot2)
                _aoes.RemoveAt(0);
        }
    }
}

class AglaeaClimbAOE(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.AglaeaClimbAOE, new AOEShapeCone(20f, 45f.Degrees()));
class Disparagement(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Disparagement, new AOEShapeCone(40f, 60f.Degrees()));

class IgnisOdi(ModuleBase module) : Components.StackWithCastTargets(module, (uint)AID.IgnisOdi, 6f, 4, 4);
class IgnisAmoris(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.IgnisAmoris, 6f);
class Frustration(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.Frustration);

class D051LiviaStates : StateMachineBuilder
{
    public D051LiviaStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<AglaeaShot>()
            .ActivateOnEnter<AglaeaClimbAOE>()
            .ActivateOnEnter<Disparagement>()
            .ActivateOnEnter<IgnisOdi>()
            .ActivateOnEnter<IgnisAmoris>()
            .ActivateOnEnter<Frustration>()
            .ActivateOnEnter<AglaeaBite>();
    }
}

[ModuleInfo(CFCID = 786u, NameID = 10290u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus, LTS) (ported from BMR)")]
public class D051Livia(WorldState ws, Actor primary) : ModuleBase(ws, primary, arena.Center, arena)
{
    private static readonly ArenaBoundsCustom arena = new([new Polygon(new(-6f, 471), 19.5f * CosPI.Pi36th, 36)], [new Rectangle(new(-6f, 491.025f), 20f, 1.25f)]);
}
