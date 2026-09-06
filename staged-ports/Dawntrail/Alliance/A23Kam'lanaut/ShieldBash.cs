// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Alliance.A23Kamlanaut;

sealed class ShieldBash(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.ShieldBash, 30f, stopAfterWall: true)
{
    public RelSimplifiedComplexPolygon Polygon;
    public bool PolygonInit;

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            if (!IsImmune(slot, act))
            {
                if (!PolygonInit)
                {
                    Polygon = Bounds.Shape.Offset(-1f); // pretend polygon is 1y smaller than real for less suspect knockbacks
                    PolygonInit = true;
                }

                hints.AddForbiddenZone(new SDKnockbackInComplexPolygonAwayFromOrigin(Center, c.Origin, 30f, Polygon), act);
            }
        }
    }
}
