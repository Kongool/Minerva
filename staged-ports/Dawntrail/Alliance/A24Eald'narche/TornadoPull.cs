// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A24Ealdnarche;

sealed class TornadoPull(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.TornadoPull, 16f, minDistance: 1f, kind: Kind.TowardsOrigin, stopAfterWall: true)
{
    private readonly TornadoFlareBurst _aoe = module.FindComponent<TornadoFlareBurst>()!;
    private readonly ArenaChanges _arena = module.FindComponent<ArenaChanges>()!;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            if (!IsImmune(slot, act))
            {
                var aoes = CollectionsMarshal.AsSpan(_aoe.Casters);
                var len = aoes.Length;
                var origins = new WPos[len];
                for (var i = 0; i < len; ++i)
                {
                    ref var aoe = ref aoes[i];
                    origins[i] = aoe.Origin;
                }
                var arenaTile = _arena.RemovedTiles.Count != 0 ? _arena.RemovedTiles[0] : 0;
                var centerTile = new WPos(784f + arenaTile % 3 * 16f, -816f + arenaTile / 3 * 16f);
                hints.AddForbiddenZone(new SDKnockbackTowardsOriginPlusAOECirclesPlusAABBSquareIntersection(Center, 16f, origins, 6f, centerTile, 9f, len), act);
            }
        }
    }

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        var aoes = CollectionsMarshal.AsSpan(_aoe.Casters);
        var len = aoes.Length;
        for (var i = 0; i < len; ++i)
        {
            ref var aoe = ref aoes[i];
            if (aoe.Check(pos))
            {
                return true;
            }
        }
        return !InBounds(pos);
    }
}
