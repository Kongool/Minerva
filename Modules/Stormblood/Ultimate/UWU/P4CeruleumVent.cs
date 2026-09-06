// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Stormblood.Ultimate.UWU;

class P4CeruleumVent(ModuleBase module) : Components.GenericAOEs(module, (uint)AID.CeruleumVent)
{
    private Actor? _source;
    private DateTime _activation;

    private static readonly AOEShapeCircle _shape = new(14f);

    public bool Active => _source != null;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_source != null)
            return new AOEInstance[1] { new(_shape, _source.Position, _source.Rotation, _activation) };
        return [];
    }

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (actor.OID == (uint)OID.UltimaWeapon && id == 0x1E43)
        {
            _source = actor;
            _activation = World.FutureTime(10.1d);
        }
    }
}
