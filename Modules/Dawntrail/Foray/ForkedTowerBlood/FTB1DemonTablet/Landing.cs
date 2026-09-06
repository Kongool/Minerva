// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerBlood.FTB1DemonTablet;

sealed class LandingKnockback(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.LandingKB1, 25f, false, shape: new AOEShapeRect(30f, 15f), kind: Kind.DirForward, stopAfterWall: true)
{
    private readonly RayOfIgnorance _aoe = module.FindComponent<RayOfIgnorance>()!;

    public override bool DestinationUnsafe(int slot, Actor actor, WPos pos)
    {
        if (_aoe.Casters.Count != 0 && _aoe.Casters.Ref(0).Check(pos))
        {
            return true;
        }
        return !InBounds(pos);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        if (Casters.Count != 0)
        {
            if (_aoe.Casters.Count != 0 && _aoe.Casters.Ref(0).Check(actor.Position))
            {
                return; // on dangerous side
            }
            var activeKnockbacks = ActiveKnockbacks(slot, actor);
            var len = activeKnockbacks.Length;
            ref readonly var c = ref Casters.Ref(0);
            var act = c.Activation;
            for (var i = 0; i < len; ++i)
            {
                ref readonly var kb = ref activeKnockbacks[i];
                if (IsImmune(slot, act))
                    return; // this won't affect player due to immunity
                if (!kb.Shape!.Check(actor.Position, kb.Origin, kb.Direction))
                    continue; // this won't affect player due to being out of aoe
                var dir = kb.Direction.ToDirection();
                hints.AddForbiddenZone(new SDInvertedRect(Center + 3f * dir, dir, 5f, default, 15f), act);
                return;
            }
        }
    }
}

sealed class RayOfIgnorance(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.RayOfIgnorance, new AOEShapeRect(30f, 15f));
sealed class LandingCircle(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Landing3, 18f);
sealed class LandingSmall(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Landing1, new AOEShapeRect(6f, 15f));
sealed class LandingMedium(ModuleBase module) : Components.SimpleAOEs(module, (uint)AID.Landing2, new AOEShapeRect(15f, 15f));
