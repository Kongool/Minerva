// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;


namespace Minerva.Endwalker.DeepDungeon.EurekaOrthos.DD40TwintaniasClone;

public enum OID : uint
{
    Boss = 0x3D1D, // R6.0
    Twister = 0x1E8910, // R0.5
    BitingWind = 0x3D1E, // R1.0
    Helper = 0x233C
}

public enum AID : uint
{
    AutoAttack = 6497, // Boss->player, no cast, single-target
    TwisterVisual = 31468, // Boss->self, 5.0s cast, single-target
    TwisterTouch = 31470, // Helper->player, no cast, single-target, player got hit by twister
    MeracydianCyclone = 31462, // Boss->self, 3.0s cast, single-target
    Gust = 31463, // Helper->location, 4.0s cast, range 5 circle
    MeracydianSquallVisual = 31465, // Boss->self, 3.0s cast, single-target
    MeracydianSquall = 31466, // Helper->location, 5.0s cast, range 5 circle
    BitingWind = 31464, // BitingWind->self, no cast, range 5 circle
    Turbine = 31467, // Boss->self, 6.0s cast, range 60 circle, knockback 15, away from source
    TwistingDive = 31471 // Boss->self, 5.0s cast, range 50 width 15 rect
}

class Twister(ModuleBase module) : Components.CastTwister(module, 1.5f, (uint)OID.Twister, (uint)AID.TwisterVisual, 0.4f, 0.25f);
class BitingWind(ModuleBase module) : Components.VoidzoneAtCastTarget(module, 5f, (uint)AID.Gust, GetVoidzones, 0.9f)
{
    private static Actor[] GetVoidzones(ModuleBase module)
    {
        var enemies = module.Enemies((uint)OID.BitingWind);
        var count = enemies.Count;
        if (count == 0)
            return [];

        var voidzones = new Actor[count];
        var index = 0;
        for (var i = 0; i < count; ++i)
        {
            var z = enemies[i];
            if (z.EventState != 7)
                voidzones[index++] = z;
        }
        return voidzones[..index];
    }
}

class MeracydianSquall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.MeracydianSquall, 5f);

class TwistersHint(ModuleBase module, uint aid) : Components.CastHint(module, aid, "Twisters soon, get moving!");
class Twisters1(ModuleBase module) : TwistersHint(module, (uint)AID.TwisterVisual);
class Twisters2(ModuleBase module) : TwistersHint(module, (uint)AID.TwistingDive);
class DiveTwister(ModuleBase module) : Components.CastTwister(module, 1.5f, (uint)OID.Twister, (uint)AID.TwistingDive, 0.4f, 0.25f);

class TwistingDive(ModuleBase module) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private static readonly AOEShapeRect rect = new(50f, 7.5f);
    private bool preparing;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor == Module.PrimaryActor)
        {
            if (id == 0x1E3A)
            {
                preparing = true;
            }
            else if (preparing && id == 0x1E43)
            {
                _aoe = [new(rect, actor.Position.Quantized(), actor.Rotation, World.FutureTime(6.9d))];
            }
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.TwistingDive)
        {
            _aoe = [];
            preparing = false;
        }
    }
}

class Turbine(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Turbine, 15f, true)
{
    private readonly BitingWind _aoe = module.FindComponent<BitingWind>()!;
    private static readonly Angle a20 = 20f.Degrees();

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var component = _aoe.ActiveAOEs(slot, actor);
            var len = component.Length;
            var forbidden = new ShapeDistance[len + 1];
            forbidden[len] = new SDInvertedCircle(Center, 5f);
            for (var i = 0; i < len; ++i)
            {
                forbidden[i] = new SDCone(Center, 20f, Angle.FromDirection(component[i].Origin - Center), a20);
            }
            hints.AddForbiddenZone(new SDIntersection(forbidden), c.Activation);
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = _aoe.ActiveAOEs(slot, actor);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref readonly var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }
}

class DD40TwintaniasCloneStates : StateMachineBuilder
{
    public DD40TwintaniasCloneStates(ModuleBase module) : base(module)
    {
        TrivialPhase()
            .ActivateOnEnter<Twister>()
            .ActivateOnEnter<Twisters1>()
            .ActivateOnEnter<Twisters2>()
            .ActivateOnEnter<BitingWind>()
            .ActivateOnEnter<MeracydianSquall>()
            .ActivateOnEnter<Turbine>()
            .ActivateOnEnter<TwistingDive>()
            .ActivateOnEnter<DiveTwister>();
    }
}

[ModuleInfo(CFCID = 900u, NameID = 12263u, PrimaryActorDeathEndsEncounter = true, Maturity = ModuleMaturity.WIP, Contributors = "The Combat Reborn Team (Malediktus) (ported from BMR)")]
public class DD40TwintaniasClone(WorldState ws, Actor primary) : ModuleBase(ws, primary, new(-600f, -300f), new ArenaBoundsCircle(20f));
