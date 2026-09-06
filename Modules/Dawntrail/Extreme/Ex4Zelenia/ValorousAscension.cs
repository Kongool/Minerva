// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Extreme.Ex4Zelenia;

sealed class ValorousAscension(ModuleBase module) : Components.CastCounterMulti(module, [(uint)AID.ValorousAscension1,
(uint)AID.ValorousAscension2, (uint)AID.ValorousAscension3]);

sealed class ValorousAscensionRect(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect rect = new(40f, 4f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoes.Count > 1 ? CollectionsMarshal.AsSpan(_aoes)[..2] : [];

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x11DB && actor.OID == (uint)OID.BriarThorn)
        {
            _aoes.Add(new(rect, actor.Position.Quantized(), actor.Rotation, World.FutureTime(10.9d)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.ValorousAscensionRect)
        {
            ++NumCasts;
            if (_aoes.Count != 0)
            {
                _aoes.RemoveAt(0);
            }
        }
    }
}
