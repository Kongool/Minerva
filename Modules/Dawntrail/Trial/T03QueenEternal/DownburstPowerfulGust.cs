// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Trial.T03QueenEternal;

sealed class PowerfulGustKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.PowerfulGust, 20f, kind: Kind.DirForward, stopAfterWall: true)
{
    public RelSimplifiedComplexPolygon Polygon = T03QueenEternal.GetXArena().Polygon.Offset(-1f); // pretend polygon is 1y smaller than real for less suspect knockbacks

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            if (!IsImmune(slot, act))
            {
                hints.AddForbiddenZone(new SDKnockbackInComplexPolygonFixedDirection(Center, 20f * c.Direction.ToDirection(), Polygon), act);
            }
        }
    }
}

sealed class DownburstKB(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Downburst, 10f, stopAfterWall: true)
{
    public RelSimplifiedComplexPolygon Polygon = T03QueenEternal.GetXArena().Polygon.Offset(-0.5f); // pretend polygon is 0.5y smaller than real for less suspect knockbacks

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            if (!IsImmune(slot, act))
            {
                hints.AddForbiddenZone(new SDKnockbackInComplexPolygonAwayFromOriginPlusIntersectionTest(Center, c.Origin, 10f, Polygon), act);
            }
        }
    }
}

sealed class PowerfulGustDownburstRW(ModuleBase module) : Components.RaidwideCasts(module, [(uint)AID.PowerfulGust, (uint)AID.Downburst]);
