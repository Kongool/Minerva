// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Savage.P8S2;

class AshingBlaze(ModuleBase module) : Components.GenericAOEs(module)
{
    private WPos? _origin;
    private static readonly AOEShapeRect _shape = new(46f, 10f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_origin != null)
            return new AOEInstance[1] { new(_shape, _origin.Value, default, Module.CastFinishAt(Module.PrimaryActor.CastInfo)) };
        return [];
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        switch (spell.Action.ID)
        {
            case (uint)AID.AshingBlazeL:
                _origin = (caster.Position - new WDir(_shape.HalfWidth, 0)).Quantized();
                break;
            case (uint)AID.AshingBlazeR:
                _origin = (caster.Position + new WDir(_shape.HalfWidth, 0)).Quantized();
                break;
        }
    }

    public override void OnCastFinished(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID is (uint)AID.AshingBlazeL or (uint)AID.AshingBlazeR)
            _origin = null;
    }
}
