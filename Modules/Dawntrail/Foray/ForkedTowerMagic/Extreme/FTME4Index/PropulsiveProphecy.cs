// Ported from BossmodReborn (BSD-3; see THIRD-PARTY-NOTICES.txt). Auto-ported by tools/port_bmr_module.py;
// review the MANUAL/MISSING items the porter reported (arena bounds, any unmapped components).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Minerva;

namespace Minerva.Dawntrail.Foray.ForkedTowerMagic.Extreme.FTME4Index;

sealed class Shockwave(ModuleBase module) : Components.SimpleKnockbacks(module, (uint)AID.Shockwave, 9f, shape: new AOEShapeCircle(15f), stopAfterWall: true)
{
    private readonly List<Knockback> _knockbacksBuffer = [];
    // on 48-man replay helpers cast x2 for each of the 3 KBs, any different for lower player runs?
    // shockwave resolves roughly 1.6s before 1st AOE from Quad
    // could show earlier using Jump but maybe not useful since need to wait for Quadrilogy/Sealed
    public RelSimplifiedComplexPolygon Polygon;
    public bool PolygonInit;

    public override ReadOnlySpan<Knockback> ActiveKnockbacks(int slot, Actor actor)
    {
        var count = Casters.Count;
        if (count == 0)
        {
            return [];
        }

        var knockbacks = _knockbacksBuffer;
        knockbacks.Clear();
        for (var i = 0; i < count; ++i)
        {
            // what happens if player standing in 2 circles?
            ref var kb = ref Casters.Ref(i);
            if (!IsImmune(slot, kb.Activation) && Shape!.Check(actor.Position, kb.Origin, default))
            {
                knockbacks.Add(kb);
                break;
            }
        }
        return CollectionsMarshal.AsSpan(knockbacks);
    }

    public override void AddHints(int slot, Actor actor, TextHints hints)
    {
        AddHints(slot, actor, hints, null);
    }

    public override void AddAIHints(int slot, Actor actor, PartyRolesConfig.Assignment assignment, AIHints hints)
    {
        // Dropped in the port: BMR shortens its own pending-knockback timer here (Actor.PendingKnockbacks), which only its
        // mover reads. Minerva's dodge does not use it.
        AddHints(slot, actor, null, hints);
    }

    private void AddHints(int slot, Actor actor, TextHints? textHints, AIHints? aiHints)
    {
        var kbs = ActiveKnockbacks(slot, actor);
        if (kbs.Length != 0)
        {
            if (!PolygonInit)
            {
                Polygon = Module.Bounds.Shape.Offset(-1f); // pretend polygon is 1y smaller than real for less suspect knockbacks
                PolygonInit = true;
            }

            // shockwave used during either quarilogy or sealed
            // during sealed happens with initial arena so should only have Aim x3

            var kb = kbs[0];
            //var aoes = _quad.ActiveAOEs(slot, actor);
            var _quad = Module.FindComponent<QuadrilogyOfImplements>();
            var _sealed = Module.FindComponent<SealedImplements>();
            var aoes = _quad != null ? _quad.ActiveAOEs(slot, actor) : _sealed != null ? _sealed.ActiveAOEs(slot, actor) : [];
            var aoecount = aoes.Length;
            List<AOEInstance> riskyaoes = [];
            for (var i = 0; i < aoecount; ++i)
            {
                var aoe = aoes[i];
                if (aoe.Risky)
                {
                    riskyaoes.Add(aoe);
                }
            }
            var riskycount = riskyaoes.Count;
            var distance = Distance + (aiHints != null ? 1f : 0f);

            ShapeDistance? sd = riskycount switch
            {
                // Module.Center slightly off from Module.PrimaryActor.Position since height isn't even
                // need to use Arena otherwise knockback polygon borked
                0 => new SDKnockbackInComplexPolygonAwayFromOrigin(Module.Center, kb.Origin, distance, Polygon),
                1 => new SDKnockbackInComplexPolygonAwayFromOriginMixedAOEs(Module.Center, kb.Origin, distance, Polygon, [.. riskyaoes], riskycount),
                3 => new SDKnockbackInComplexPolygonAwayFromOriginMixedAOEs(Module.Center, kb.Origin, distance, Polygon, [.. riskyaoes], riskycount),
                _ => null
            };

            if (sd == null)
            {
                return;
            }

            if (textHints != null)
            {
                if (sd.Contains(actor.Position))
                {
                    textHints?.Add("About to be knocked into danger!");
                }
            }
            else
            {
                // avoid circle from other 2 knockbacks
                var kbCount = Casters.Count;
                for (var i = 0; i < kbCount; ++i)
                {
                    ref var other = ref Casters.Ref(i);
                    var origin = other.Origin;
                    if (!origin.AlmostEqual(kb.Origin, 1f))
                    {
                        aiHints?.AddForbiddenZone(new SDCircle(origin, 16f), kb.Activation);
                    }
                }

                aiHints?.AddForbiddenZone(sd, kb.Activation);
            }
        }
    }
}
