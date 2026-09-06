// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V2MountRokkon.V23Gorai;

sealed class Thundercall(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<AOEInstance> _aoes = [];
    private int counter;
    private static readonly AOEShapeCircle circleSmall = new(8f), circleBig = new(18f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (counter < 2 && actor.OID == (uint)OID.BallOfLevin)
        {
            _aoes.Add(new(circleBig, actor.Position.Quantized(), default, World.FutureTime(10.8d)));
            ++counter;
        }
    }

    public override void OnStatusGain(Actor actor, ref ActorStatus status)
    {

        if (actor.OID == (uint)OID.BallOfLevin && status.ID == (uint)SID.SmallOrb)
        {
            var activation = World.FutureTime(10d);
            AddAOE(circleSmall, actor.Position);

            var orbs = Module.Enemies((uint)OID.BallOfLevin);
            var count = orbs.Count;
            for (var i = 0; i < count; ++i)
            {
                var orb = orbs[i];
                if (orb != actor)
                {
                    AddAOE(circleBig, orb.Position);
                }
            }
            void AddAOE(AOEShape shape, WPos origin) => _aoes.Add(new(shape, origin.Quantized(), default, activation));
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.ShockSmall or (uint)AID.ShockLarge)
        {
            _aoes.Clear();
        }
    }
}
