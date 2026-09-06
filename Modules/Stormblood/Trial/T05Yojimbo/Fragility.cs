// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class Fragility(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeCircle _circle = new(8f);
    private const double _fragilityDelay = 4.85d;
    public readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.Inoshikacho)
            _aoes.Add(new(_circle, actor.Position.Quantized(), actor.Rotation, World.FutureTime(_fragilityDelay)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.Fragility)
            _aoes.Clear();
    }
}
