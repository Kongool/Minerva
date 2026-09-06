// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class P3Burst(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.Burst)
{
    private readonly List<Actor> _bombs = module.Enemies((uint)OID.BombBoulder);
    private readonly Dictionary<ulong, DateTime?> _bombActivation = [];

    private static readonly AOEShape _shape = new AOEShapeCircle(6.3f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _bombs.Count;
        if (count == 0)
            return [];
        var aoes = new List<AOEInstance>();
        for (var i = 0; i < count; ++i)
        {
            var b = _bombs[i];
            var activation = _bombActivation.GetValueOrDefault(b.InstanceID);
            if (activation != null)
                aoes.Add(new(_shape, b.Position, b.Rotation, Module.CastFinishAt(b.CastInfo, 0, activation.Value)));
        }
        return CollectionsMarshal.AsSpan(aoes);
    }

    public override void Update()
    {
        var count = _bombs.Count;
        if (count == 0)
            return;
        for (var i = 0; i < count; ++i)
        {
            var b = _bombs[i];
            if (!_bombActivation.ContainsKey(b.InstanceID))
                _bombActivation[b.InstanceID] = World.FutureTime(6.5d);
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
            _bombActivation[caster.InstanceID] = null;
    }
}
