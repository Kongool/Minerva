// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Ultimate.DSW2;

// used by two trio mechanics, in p2 and in p5
abstract class HeavyImpact(ModuleBase module, double activationDelay) : Components.GenericAOEs(module)
{
    private AOEInstance[] _aoe = [];
    private readonly double _activationDelay = activationDelay;

    private const float _impactRadiusIncrement = 6f;

    public bool Active => _aoe.Length != 0;

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => _aoe;

    public override void OnActorPlayActionTimelineEvent(Actor actor, ushort id)
    {
        if (id == 0x1E43 && actor.OID == (uint)OID.SerGuerrique)
        {
            _aoe = [new(new AOEShapeCircle(_impactRadiusIncrement), actor.Position, default, World.FutureTime(_activationDelay))];
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.HeavyImpactHit1 or (uint)AID.HeavyImpactHit2 or (uint)AID.HeavyImpactHit3 or (uint)AID.HeavyImpactHit4 or (uint)AID.HeavyImpactHit5)
        {
            if (++NumCasts < 5)
            {
                var inner = _impactRadiusIncrement * NumCasts;
                _aoe = [new(new AOEShapeDonut(inner, inner + _impactRadiusIncrement), caster.Position, default, World.FutureTime(1.9d))];
            }
            else
            {
                _aoe = [];
            }
        }
    }
}
