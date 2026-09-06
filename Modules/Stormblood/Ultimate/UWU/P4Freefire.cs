// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class P4Freefire(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.FreefireIntermission)
{
    private readonly List<AOEInstance> _aoes = [];

    private static readonly AOEShape _shape = new AOEShapeCircle(15); // TODO: verify falloff

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => CollectionsMarshal.AsSpan(_aoes);

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.Helper && id == 0x0449)
        {
            _aoes.Add(new(_shape, actor.Position.Quantized(), default, World.FutureTime(5.9d)));
        }
    }
}
