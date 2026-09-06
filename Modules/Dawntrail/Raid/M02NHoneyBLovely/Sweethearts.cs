// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Raid.M02NHoneyBLovely;

abstract class Sweethearts(ModuleBase module, uint oid, uint aid) : Components.GenericAOEs(module)
{
    private const float Radius = 1f, Length = 3f;
    private static readonly AOEShapeCapsule capsule = new(Radius, Length);
    private readonly List<Actor> _hearts = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _hearts.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
        {
            var h = _hearts[i];
            aoes[i] = new(capsule, h.Position, h.Rotation);
        }
        return aoes;
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == oid && id == 0x11D3)
            _hearts.Add(actor);
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID == oid)
            _hearts.Remove(actor);
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == aid)
            _hearts.Remove(caster);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        var count = _hearts.Count;
        if (count == 0)
            return;
        var forbiddenImminent = new ShapeDistance[count + 1];
        var forbiddenFuture = new ShapeDistance[count];
        for (var i = 0; i < count; ++i)
        {
            var h = _hearts[i];
            forbiddenFuture[i] = new SDCapsule(h.Position, h.Rotation, Length, Radius);
            forbiddenImminent[i] = new SDCircle(h.Position, Radius);
        }
        forbiddenImminent[count] = new SDCircle(Center, Module.PrimaryActor.HitboxRadius);

        hints.AddForbiddenZone(new SDUnion(forbiddenFuture), World.FutureTime(1.5d));
        hints.AddForbiddenZone(new SDUnion(forbiddenImminent));
    }
}

sealed class SweetheartsN(ModuleBase module) : Sweethearts(module, (uint)OID.Sweetheart, (uint)AID.SweetheartTouch);
