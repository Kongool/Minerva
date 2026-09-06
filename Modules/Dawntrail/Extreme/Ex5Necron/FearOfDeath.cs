// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex5Necron;

sealed class FearOfDeath(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.FearOfDeath);

sealed class FearOfDeathAOE(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeCircle circle = new(3);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.IcyHands1)
        {
            _aoes.Add(new(circle, actor.Position.Quantized(), default, World.FutureTime(7.8d)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.FearOfDeathAOE1)
        {
            ++NumCasts;
        }
    }
}

sealed class ChokingGraspBait1(ModuleBase module) : Components.GenericBaitAway(module)
{
    private DateTime _activation;
    private readonly List<Actor> sources = [];

    public override void Update()
    {
        CurrentBaits.Clear();
        if (_activation != default)
        {
            var party = Raid.WithoutSlot(false, true, true);
            var count = sources.Count;
            var len = party.Length;
            for (var i = 0; i < count; ++i)
            {
                var source = sources[i];
                Actor? closest = null;
                var closestDistSq = float.MaxValue;

                for (var j = 0; j < len; ++j)
                {
                    var player = party[j];
                    var distSq = (player.Position - source.Position).LengthSq();
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closest = player;
                    }
                }

                if (closest != null)
                {
                    CurrentBaits.Add(new(source, closest, ChokingGrasp.Rect, _activation));
                }
            }
        }
    }

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.IcyHands1)
        {
            sources.Add(actor);
            _activation = World.FutureTime(10.5d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.ChokingGraspBait)
        {
            ++NumCasts;
        }
    }
}

sealed class ChokingGraspBait2(ModuleBase module) : Components.GenericBaitAway(module)
{
    private DateTime _activation;
    private readonly List<Actor> sources = [];

    public override void Update()
    {
        CurrentBaits.Clear();
        if (_activation != default)
        {
            var party = Raid.WithoutSlot(false, true, true);
            var count = sources.Count;
            var len = party.Length;
            for (var i = 0; i < count; ++i)
            {
                var source = sources[i];
                Actor? closest = null;
                var closestDistSq = float.MaxValue;

                for (var j = 0; j < len; ++j)
                {
                    var player = party[j];
                    var distSq = (player.Position - source.Position).LengthSq();
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closest = player;
                    }
                }

                if (closest != null)
                {
                    CurrentBaits.Add(new(source, closest, ChokingGrasp.Rect, _activation));
                }
            }
        }
    }

    public override void OnActorModelStateChange(Actor actor, byte modelState, byte animState1, byte animState2)
    {
        if (animState1 == 1 && actor.OID == (uint)OID.IcyHands1)
        {
            sources.Add(actor);
            _activation = World.FutureTime(2.5d);
        }
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ChokingGraspAOE1)
        {
            ++NumCasts;
        }
    }
}

sealed class TheEndsEmbrace(ModuleBase module) : Components.SpreadFromIcon(module, (uint)IconID.TheEndsEmbrace, (uint)AID.TheEndsEmbrace, 3f, 4.1f);
