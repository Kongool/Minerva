// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Shadowbringers.Ultimate.TEA;

[SkipLocalsInit]
sealed class P2EyeOfTheChakram(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.EyeOfTheChakram)
{
    private readonly List<AOEInstance> _aoes = [];
    private readonly AOEShapeRect rect = new(76f, 3f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorCreated(Actor actor)
    {
        if (actor.OID == (uint)OID.SteamChakram)
        {
            _aoes.Add(new(rect, actor.Position.Quantized(), actor.Rotation, World.FutureTime(7.9d)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == WatchedAction)
        {
            _aoes.Clear();
        }
    }
}
