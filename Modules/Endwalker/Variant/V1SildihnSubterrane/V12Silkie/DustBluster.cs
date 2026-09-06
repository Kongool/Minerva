// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Endwalker.VariantCriterion.V1SildihnSubterrane.V12Silkie;

sealed class DustBluster(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.DustBluster, 16f)
{
    private readonly List<Actor> waterVZs = module.Enemies((uint)OID.WaterVoidzone);

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            ref readonly var kb = ref Casters.Ref(0);
            var act = kb.Activation;
            if (!IsImmune(slot, act))
            {
                var origin = kb.Origin;
                var count = waterVZs.Count;
                var forbidden = new ShapeDistance[count + 1];

                // square intentionally slightly smaller to prevent sus knockback
                forbidden[count] = new SDKnockbackInAABBSquareAwayFromOrigin(Center, origin, 16f, 19f);

                for (var i = 0; i < count; ++i)
                {
                    var a = waterVZs[i].Position;
                    forbidden[i] = new SDCone(origin, 100f, Angle.FromDirection(a - origin), Angle.Asin(5f / (a - origin).Length()));
                }
                hints.AddForbiddenZone(new SDUnion(forbidden), act);
            }
        }
    }
}
