// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex3QueenEternal;

sealed class DivideAndConquerBait(ModuleBase module) : Components.GenericBaitAway(module, (uint)AID.DivideAndConquerBait)
{
    private static readonly AOEShapeRect _shape = new(60f, 2.5f);

    public override void OnEventIcon(Actor actor, uint iconID, ulong targetID)
    {
        if (iconID == (uint)IconID.DivideAndConquer && World.Actors.Find(targetID) is var target && target != null)
            CurrentBaits.Add(new(actor, target, _shape, World.FutureTime(3.1d)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            ++NumCasts;
            if (CurrentBaits.Count != 0)
                CurrentBaits.RemoveAt(0);
        }
    }
}

sealed class DivideAndConquerAOE(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.DivideAndConquerBait)
{
    private static readonly AOEShapeRect rect = new(60f, 2.5f);
    public readonly List<AOEInstance> AOEs = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(AOEs);

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == WatchedAction)
            AOEs.Add(new(rect, caster.Position, caster.Rotation, World.FutureTime(11d - AOEs.Count)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.DivideAndConquerAOE)
        {
            ++NumCasts;
            AOEs.Clear();
        }
    }
}
