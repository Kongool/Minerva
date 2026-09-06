// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.Alliance.A34Eulogia;

sealed class MatronsBreath(ModuleBase module) : Components.GenericAOEs(module)
{
    private readonly List<Actor> _blueSafe = module.Enemies((uint)OID.BlueSafeZone);
    private readonly List<Actor> _goldSafe = module.Enemies((uint)OID.GoldSafeZone);
    private readonly List<AOEInstance> _flowers = [];

    private static readonly AOEShapeDonut _shape = new(8f, 50f);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        if (_flowers.Count != 0)
        {
            return CollectionsMarshal.AsSpan(_flowers)[..1];
        }
        return [];
    }

    public override void OnActorCreated(Actor actor)
    {
        var safezone = actor.OID switch
        {
            (uint)OID.BlueFlowers => _blueSafe[0],
            (uint)OID.GoldFlowers => _goldSafe[0],
            _ => null
        };
        if (safezone != null)
            _flowers.Add(new(_shape, safezone.Position, default, World.FutureTime(11d)));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.Blueblossoms or (uint)AID.Giltblossoms)
        {
            ++NumCasts;
            if (_flowers.Count != 0)
            {
                _flowers.RemoveAt(0);
            }
        }
    }
}
