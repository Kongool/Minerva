// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Savage.M12SLindwurm;

sealed class Burst(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle _shape = new(12f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorEAnim(Actor actor, uint state)
    {
        if (actor.OID == (uint)OID.Actor1ebf29 && state is 0x100020 or 0x10002)
            _aoes.Add(new(_shape, actor.Position.Quantized(), default, World.FutureTime(2.2f)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.Burst)
            _aoes.Clear();
    }
}
