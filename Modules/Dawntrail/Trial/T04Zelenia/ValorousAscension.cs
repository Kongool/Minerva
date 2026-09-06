// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T04Zelenia;

sealed class ValorousAscension(ModuleBase module) : Components.RaidwideCast(module, (uint)AID.ValorousAscension1, "Raidwide x3");

sealed class ValorousAscensionRect(ModuleBase module) : Components.GenericAOEs(module)
{
    private static readonly AOEShapeRect rect = new(40f, 4f);
    private readonly List<AOEInstance> _aoes = [];

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoes.Count > 1 ? CollectionsMarshal.AsSpan(_aoes)[..2] : [];

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x11DB && actor.OID == (uint)OID.BriarThorn1)
        {
            _aoes.Add(new(rect, actor.Position.Quantized(), actor.Rotation, World.FutureTime(11.1d)));
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (_aoes.Count != 0 && spell.Action.ID == (uint)AID.ValorousAscensionRect)
        {
            _aoes.RemoveAt(0);
        }
    }
}
