// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S2;

class EndOfDays(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.EndOfDays)
{
    public List<(Actor caster, DateTime finish)> Casters = [];

    private static readonly AOEShapeRect _shape = new(60f, 5f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = Casters.Count;
        if (count == 0)
            return [];

        var max = count > 3 ? 3 : count;
        var aoes = new AOEInstance[max];

        for (var i = 0; i < max; ++i)
        {
            var c = Casters[i];
            var rotation = c.caster.CastInfo?.Rotation ?? c.caster.Rotation;
            var finish = c.caster.CastInfo != null ? Module.CastFinishAt(c.caster.CastInfo) : c.finish;

            aoes[i] = new(_shape, c.caster.Position, rotation, finish);
        }

        return aoes;
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            Casters.RemoveAll(c => c.caster == caster);
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.IllusoryHephaistosLanes && id == 0x11D3)
            Casters.Add((actor, World.FutureTime(8))); // ~2s before cast start
    }
}
