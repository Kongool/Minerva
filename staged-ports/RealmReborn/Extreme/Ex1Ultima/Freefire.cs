// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.RealmReborn.Extreme.Ex1Ultima;

class Freefire(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.Freefire)
{
    private readonly List<Actor> _casters = [];
    private DateTime _resolve;
    public bool Active => _casters.Count > 0;

    private static readonly AOEShapeCircle _shape = new(15f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _casters.Count;
        if (count == 0)
            return [];
        var aoes = new AOEInstance[count];
        for (var i = 0; i < count; ++i)
            aoes[i] = new(_shape, _casters[i].Position, default, _resolve);
        return aoes;
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Active && Module.PrimaryActor.TargetID == actor.InstanceID && NumCasts > 0)
        {
            // for second set, let current MT stay in place and use invuln instead of risking cleaving the raid
            var invuln = actor.Class switch
            {
                Class.WAR => ActionID.MakeSpell(WAR.AID.Holmgang),
                Class.PLD => ActionID.MakeSpell(PLD.AID.HallowedGround),
                _ => default
            };
            if (invuln.IsValid)
            {
                hints.ActionsToExecute.Push(invuln, actor, ActionQueue.Priority.High, (float)(_resolve - World.CurrentTime).TotalSeconds);
                return;
            }
        }

        base.AddAIHints(slot, actor, assignment, hints);
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x0449 && actor.OID == (uint)OID.Helper)
        {
            _casters.Add(actor);
            _resolve = World.FutureTime(6d);
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        base.OnEventCast(caster, spell);
        if (spell.Action.ID == WatchedAction)
            _casters.Remove(caster);
    }
}
