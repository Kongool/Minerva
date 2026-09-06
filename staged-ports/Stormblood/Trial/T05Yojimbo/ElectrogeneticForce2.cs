// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Trial.T05Yojimbo;

class ElectrogeneticForce2(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.ElectrogeneticForce2, _circle)
{
    private static readonly AOEShapeCircle _circle = new(8f);
    private const float _electrogeneticForce2Delay = 6.7f;
    public readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.ElectrogeneticForce)
            _aoes.Add(new(_circle, actor.Position, default, World.FutureTime(_electrogeneticForce2Delay)));
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ElectrogeneticForce2)
            _aoes.Clear();
    }
}
